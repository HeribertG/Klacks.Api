// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IWorkWriteGuard: refuses a real write only for a missing mandatory qualification (the one
/// Error class that cannot be reported after the fact) and for an exhausted sporadic shift. Manual
/// planning otherwise stays advisory - a Block-mode compliance rule reports into the error list rather
/// than stopping the write, and a schedule collision (owner decision 2026-08-22) does too: it is persisted
/// and the async post-commit check (ScheduleTimelineBackgroundService) surfaces it like any other finding.
/// Each refusal is a WorkWriteConflictException, so the 409 body carries an error code plus the names, counts
/// and conflict items the client needs to explain it in the user's language.
/// </summary>
/// <param name="shiftRepository">Source of the slim sporadic projection of the Work's shift</param>
/// <param name="workRepository">Source of the sporadic capacity usage in the shift's range</param>
/// <param name="conflictChecker">Replays the period validator with and without the planned row</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Shifts;

namespace Klacks.Api.Application.Services.Schedules;

public class WorkWriteGuardService : IWorkWriteGuard
{
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
        var shift = await _shiftRepository.GetSporadicInfoAsync(work.ShiftId, cancellationToken);
        if (shift is null || !shift.IsSporadic)
        {
            return;
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
            throw new WorkWriteConflictException(
                $"Sporadic shift '{shift.Name}' is fully booked on {work.CurrentDate:yyyy-MM-dd} " +
                $"({usage.EngagedAtDay}/{shift.EffectiveSumEmployees} employees).",
                WorkWriteConflictCodes.SporadicShiftDayFull,
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
            throw new WorkWriteConflictException(
                $"Sporadic shift '{shift.Name}' has reached its range capacity " +
                $"({usage.DistinctBookedDays}/{shift.EffectiveQuantity} days) for scope {shift.SporadicScope} " +
                $"({rangeFrom:yyyy-MM-dd}..{rangeUntil:yyyy-MM-dd}).",
                WorkWriteConflictCodes.SporadicShiftRangeExhausted,
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

        var conflictCheck = await _conflictChecker.CheckAsync([plannedRow], null, cancellationToken);
        if (conflictCheck.HasHardBlocking)
        {
            var shift = await _shiftRepository.GetSporadicInfoAsync(work.ShiftId, cancellationToken);

            throw new WorkWriteConflictException(
                $"Work blocked: client {work.ClientId} would introduce " +
                $"{conflictCheck.NewConflicts.Count(c => c.Type == ScheduleValidationType.Error)} " +
                $"non-overridable schedule conflict(s) on {work.CurrentDate:yyyy-MM-dd}. Not committed.",
                WorkWriteConflictCodes.BlockedByConflicts,
                new Dictionary<string, object?>
                {
                    [WorkWriteConflictCodes.ShiftIdField] = work.ShiftId,
                    [WorkWriteConflictCodes.ShiftNameField] = shift?.Name,
                    [WorkWriteConflictCodes.DateField] = work.CurrentDate,
                    [WorkWriteConflictCodes.ConflictsField] = conflictCheck.HardBlockingConflicts
                        .Select(c => new WorkConflictItem(c.Comment, c.ClientId, c.Date, c.CommentParams))
                        .ToList(),
                });
        }
    }
}
