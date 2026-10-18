// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IWorkWriteGuard: refuses a real write only for a missing mandatory qualification or one held below a binding
/// minimum level (the Error class that cannot be reported after the fact) and for an exhausted sporadic shift. Manual
/// planning otherwise stays advisory - a Block-mode compliance rule reports into the error list rather
/// than stopping the write, and a schedule collision (owner decision 2026-08-22) does too: it is persisted
/// and the async post-commit check (ScheduleTimelineBackgroundService) surfaces it like any other finding.
/// The Find methods only report a refusal; the Ensure methods throw exactly that refusal as a WorkWriteConflictException,
/// so the 409 body carries an error code plus the names, counts and conflict items the client needs to explain it in the
/// user's language, and a diagnosis reaches the same verdict as the write.
/// </summary>
/// <param name="shiftRepository">Source of the slim sporadic projection of the Work's shift</param>
/// <param name="workRepository">Source of the sporadic capacity usage in the shift's range</param>
/// <param name="conflictChecker">Replays the period validator with and without the planned row</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Services.Schedules;

public class WorkWriteGuardService : IWorkWriteGuard
{
    private const string CreateBlockedPrefix = "Work blocked";
    private const string UpdateBlockedPrefix = "Work update blocked";
    private const string ReassignBlockedPrefix = "Reassignment blocked";

    private readonly IShiftRepository _shiftRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IPreCommitConflictChecker _conflictChecker;

    public WorkWriteGuardService(
        IShiftRepository shiftRepository,
        IWorkRepository workRepository,
        IPreCommitConflictChecker conflictChecker)
    {
        _shiftRepository = shiftRepository;
        _workRepository = workRepository;
        _conflictChecker = conflictChecker;
    }

    public async Task EnsureNoSporadicConflictAsync(Work work, CancellationToken cancellationToken)
    {
        var refusal = await FindSporadicRefusalAsync(work, cancellationToken);
        if (refusal != null)
        {
            throw WorkWriteConflictException.From(refusal);
        }
    }

    public async Task<WorkBookingRefusal?> FindSporadicRefusalAsync(Work work, CancellationToken cancellationToken)
    {
        var shift = await _shiftRepository.GetSporadicInfoAsync(work.ShiftId, cancellationToken);
        if (shift is null || !shift.IsSporadic)
        {
            return null;
        }

        var (rangeFrom, rangeUntil) = SporadicRangeCalculator.Compute(shift, work.CurrentDate);

        var usage = await _workRepository.GetSporadicCapacityUsageAsync(
            work.ShiftId,
            work.CurrentDate,
            rangeFrom,
            rangeUntil,
            excludeWorkId: work.Id == Guid.Empty ? null : work.Id,
            work.AnalyseToken,
            cancellationToken);

        if (usage.EngagedAtDay >= shift.EffectiveSumEmployees)
        {
            return new WorkBookingRefusal(
                WorkWriteConflictCodes.SporadicShiftDayFull,
                $"Sporadic shift '{shift.Name}' is fully booked on {work.CurrentDate:yyyy-MM-dd} " +
                $"({usage.EngagedAtDay}/{shift.EffectiveSumEmployees} employees).",
                new Dictionary<string, object?>
                {
                    [WorkWriteConflictCodes.ShiftIdField] = work.ShiftId,
                    [WorkWriteConflictCodes.ShiftNameField] = shift.Name,
                    [WorkWriteConflictCodes.DateField] = work.CurrentDate,
                    [WorkWriteConflictCodes.EngagedField] = usage.EngagedAtDay,
                    [WorkWriteConflictCodes.CapacityField] = shift.EffectiveSumEmployees,
                });
        }

        if (usage.EngagedAtDay == 0 && usage.DistinctBookedDays >= shift.EffectiveQuantity)
        {
            return new WorkBookingRefusal(
                WorkWriteConflictCodes.SporadicShiftRangeExhausted,
                $"Sporadic shift '{shift.Name}' has reached its range capacity " +
                $"({usage.DistinctBookedDays}/{shift.EffectiveQuantity} days) for scope {shift.SporadicScope} " +
                $"({rangeFrom:yyyy-MM-dd}..{rangeUntil:yyyy-MM-dd}).",
                new Dictionary<string, object?>
                {
                    [WorkWriteConflictCodes.ShiftIdField] = work.ShiftId,
                    [WorkWriteConflictCodes.ShiftNameField] = shift.Name,
                    [WorkWriteConflictCodes.DateField] = work.CurrentDate,
                    [WorkWriteConflictCodes.BookedField] = usage.DistinctBookedDays,
                    [WorkWriteConflictCodes.CapacityField] = shift.EffectiveQuantity,
                    [WorkWriteConflictCodes.RangeFromField] = rangeFrom,
                    [WorkWriteConflictCodes.RangeUntilField] = rangeUntil,
                });
        }

        return null;
    }

    public async Task EnsureNoHardBlockingConflictAsync(Work work, CancellationToken cancellationToken)
    {
        if (work.AnalyseToken != null)
        {
            return;
        }

        var plannedRow = new PlannedWorkRow(
            work.ClientId,
            work.CurrentDate,
            work.StartTime,
            work.EndTime,
            work.ShiftId);

        var refusal = await FindHardBlockingRefusalAsync([plannedRow], null, WorkWriteOperation.Create, cancellationToken);
        if (refusal != null)
        {
            throw WorkWriteConflictException.From(refusal);
        }
    }

    public async Task<WorkBookingRefusal?> FindHardBlockingRefusalAsync(
        IReadOnlyList<PlannedWorkRow> plannedRows,
        IReadOnlyList<PlannedRemovalRow>? vacatedRows,
        WorkWriteOperation operation,
        CancellationToken cancellationToken)
    {
        if (plannedRows.Count == 0)
        {
            return null;
        }

        var conflictCheck = vacatedRows is null
            ? await _conflictChecker.CheckAsync(plannedRows, null, cancellationToken)
            : await _conflictChecker.CheckAsync(plannedRows, vacatedRows, null, cancellationToken);
        if (!conflictCheck.HasHardBlocking)
        {
            return null;
        }

        var errorCount = conflictCheck.NewConflicts.Count(c => c.Type == ScheduleValidationType.Error);
        var shiftIds = plannedRows.Select(r => r.ShiftId).Distinct().ToList();
        var dates = plannedRows.Select(r => r.Date).Distinct().ToList();
        Guid? shiftId = shiftIds.Count == 1 ? shiftIds[0] : null;
        DateOnly? date = dates.Count == 1 ? dates[0] : null;
        var shift = shiftId.HasValue
            ? await _shiftRepository.GetSporadicInfoAsync(shiftId.Value, cancellationToken)
            : null;

        return new WorkBookingRefusal(
            WorkWriteConflictCodes.BlockedByConflicts,
            HardBlockingMessage(operation, plannedRows[0], errorCount),
            new Dictionary<string, object?>
            {
                [WorkWriteConflictCodes.ShiftIdField] = shiftId,
                [WorkWriteConflictCodes.ShiftNameField] = shift?.Name,
                [WorkWriteConflictCodes.DateField] = date,
                [WorkWriteConflictCodes.ConflictsField] = conflictCheck.HardBlockingConflicts
                    .Select(c => new WorkConflictItem(c.Comment, c.ClientId, c.Date, c.CommentParams))
                    .ToList(),
            });
    }

    private static string HardBlockingMessage(WorkWriteOperation operation, PlannedWorkRow row, int errorCount)
        => operation switch
        {
            WorkWriteOperation.Bulk =>
                "Bulk write blocked: at least one row would introduce " +
                $"{errorCount} non-overridable schedule conflict(s). Nothing was committed.",
            WorkWriteOperation.Create => SingleRowMessage(CreateBlockedPrefix, row, errorCount),
            WorkWriteOperation.Update => SingleRowMessage(UpdateBlockedPrefix, row, errorCount),
            WorkWriteOperation.Reassign => SingleRowMessage(ReassignBlockedPrefix, row, errorCount),
            _ => throw new ArgumentOutOfRangeException(nameof(operation), operation, null),
        };

    private static string SingleRowMessage(string prefix, PlannedWorkRow row, int errorCount)
        => $"{prefix}: client {row.ClientId} would introduce {errorCount} " +
           $"non-overridable schedule conflict(s) on {row.Date:yyyy-MM-dd}. Not committed.";
}
