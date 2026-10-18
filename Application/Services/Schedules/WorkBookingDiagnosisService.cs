// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IWorkBookingDiagnosisService. Runs the checks of the direct booking path (Works POST) in its order and with
/// its own primitives, so the first direct-entry refusal carries exactly the code the real POST would answer with:
/// visibility, day seal (skipped for a scenario, like the day lock), membership and group period, sporadic capacity,
/// hard-blocking conflict (skipped for a scenario). On top it reports what the assistant's placement refuses (a sealed
/// day also in a scenario, every Error-level conflict, but no sporadic capacity check) and what the schedule grid blocks
/// without a request (an occupied cell). A hidden or missing employee, shift or scenario ends the diagnosis before any
/// other query and reads exactly like "not found"; an occupying work on a shift the caller cannot see is reported
/// without its shift name or lock level. The seal kind (day approval or period close) is read only to word the remedy,
/// never to decide whether the day is sealed. The conflict replay runs twice for the real plan (once inside the shared
/// hard-blocking check, once for the assistant's view and the warnings); a chat question accepts that cost.
/// </summary>
/// <param name="rowAccessGuard">Decides whether the caller may see the employee and the scenario</param>
/// <param name="clientRepository">Loads the employee's name; its query filter hides deleted rows</param>
/// <param name="shiftVisibilityGuard">Decides whether the shift exists and the caller may book it</param>
/// <param name="shiftRepository">Loads the shift's name and times</param>
/// <param name="sealedDayRepository">Answers whether the day is sealed for the employee, and by which seal kind</param>
/// <param name="planningPeriodGuard">Names a day outside the employee's membership or group period</param>
/// <param name="writeGuard">The sporadic and hard-blocking checks the write paths throw from</param>
/// <param name="conflictChecker">Replays the period validator for the assistant's view and the warnings</param>
/// <param name="workRepository">Reads the works already in the cell</param>
/// <param name="breakRepository">Reads the absences already in the cell</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class WorkBookingDiagnosisService : IWorkBookingDiagnosisService
{
    private static readonly IReadOnlyDictionary<string, object?> NoDetails = new Dictionary<string, object?>();

    private readonly IScheduleRowAccessGuard _rowAccessGuard;
    private readonly IClientRepository _clientRepository;
    private readonly IShiftVisibilityGuard _shiftVisibilityGuard;
    private readonly IShiftRepository _shiftRepository;
    private readonly ISealedDayRepository _sealedDayRepository;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly IWorkWriteGuard _writeGuard;
    private readonly IPreCommitConflictChecker _conflictChecker;
    private readonly IWorkRepository _workRepository;
    private readonly IBreakRepository _breakRepository;

    public WorkBookingDiagnosisService(
        IScheduleRowAccessGuard rowAccessGuard,
        IClientRepository clientRepository,
        IShiftVisibilityGuard shiftVisibilityGuard,
        IShiftRepository shiftRepository,
        ISealedDayRepository sealedDayRepository,
        IClientPlanningPeriodGuard planningPeriodGuard,
        IWorkWriteGuard writeGuard,
        IPreCommitConflictChecker conflictChecker,
        IWorkRepository workRepository,
        IBreakRepository breakRepository)
    {
        _rowAccessGuard = rowAccessGuard;
        _clientRepository = clientRepository;
        _shiftVisibilityGuard = shiftVisibilityGuard;
        _shiftRepository = shiftRepository;
        _sealedDayRepository = sealedDayRepository;
        _planningPeriodGuard = planningPeriodGuard;
        _writeGuard = writeGuard;
        _conflictChecker = conflictChecker;
        _workRepository = workRepository;
        _breakRepository = breakRepository;
    }

    public async Task<WorkBookingDiagnosis> DiagnoseAsync(
        WorkBookingDiagnosisRequest request, CancellationToken cancellationToken)
    {
        if (!await _rowAccessGuard.IsVisibleAsync(request.ClientId, request.AnalyseToken, cancellationToken))
        {
            return WorkBookingDiagnosis.NotFound;
        }

        var client = await _clientRepository.Get(request.ClientId);
        if (client == null || !await _shiftVisibilityGuard.ExistsAndVisibleAsync(request.ShiftId, cancellationToken))
        {
            return WorkBookingDiagnosis.NotFound;
        }

        var shift = await _shiftRepository.Get(request.ShiftId);
        if (shift == null)
        {
            return WorkBookingDiagnosis.NotFound;
        }

        var inScenario = request.AnalyseToken.HasValue;
        var refusals = new List<WorkBookingRefusalFinding>();

        var seal = await FindSealAsync(request, inScenario, cancellationToken);
        AddIfPresent(refusals, seal);
        AddIfPresent(refusals, await FindPeriodRefusalAsync(request, cancellationToken));

        var work = new Work
        {
            ClientId = request.ClientId,
            ShiftId = request.ShiftId,
            CurrentDate = request.Date,
            StartTime = shift.StartShift,
            EndTime = shift.EndShift,
            AnalyseToken = request.AnalyseToken,
        };
        AddIfPresent(refusals, ToSporadicFinding(await _writeGuard.FindSporadicRefusalAsync(work, cancellationToken)));

        var plannedRow = new PlannedWorkRow(request.ClientId, request.Date, shift.StartShift, shift.EndShift, request.ShiftId);
        var directHardBlocking = inScenario
            ? null
            : await _writeGuard.FindHardBlockingRefusalAsync([plannedRow], null, WorkWriteOperation.Create, cancellationToken);
        var assistantCheck = await _conflictChecker.CheckAsync([plannedRow], request.AnalyseToken, cancellationToken);
        AddIfPresent(refusals, ToHardBlockingFinding(directHardBlocking, assistantCheck));
        AddIfPresent(refusals, ToAssistantOnlyFinding(assistantCheck));

        refusals.AddRange(await FindOccupyingWorksAsync(request, cancellationToken));
        AddIfPresent(refusals, await FindOccupyingAbsenceAsync(request, inScenario, cancellationToken));

        var advisories = assistantCheck.NewConflicts
            .Where(conflict => conflict.Type != ScheduleValidationType.Error)
            .Select(conflict => new WorkBookingAdvisory(conflict.Comment, conflict.Type, conflict.Date, conflict.CommentParams))
            .ToList();

        return new WorkBookingDiagnosis(
            OutcomeOf(refusals), $"{client.FirstName} {client.Name}".Trim(), shift.Name, refusals, advisories);
    }

    private async Task<WorkBookingRefusalFinding?> FindSealAsync(
        WorkBookingDiagnosisRequest request, bool inScenario, CancellationToken cancellationToken)
    {
        if (!await _sealedDayRepository.IsDayLockedAsync(request.Date, request.ClientId, cancellationToken))
        {
            return null;
        }

        var closed = await _sealedDayRepository.GetLockedPairsAsync(
            [(request.Date, request.ClientId)], WorkLockLevel.Closed, cancellationToken);
        var kind = closed.Count > 0 ? WorkBookingSealKind.PeriodClosed : WorkBookingSealKind.DayApproval;

        return new WorkBookingRefusalFinding(
            WorkBookingRefusalReason.DaySealed,
            WorkBookingRefusalCodes.DaySealed,
            new WorkBookingChannels(DirectEntry: !inScenario, Assistant: true, Grid: false),
            kind == WorkBookingSealKind.PeriodClosed ? WorkBookingRemedy.ReopenPeriodAdmin : WorkBookingRemedy.RevokeDayApproval,
            new Dictionary<string, object?>
            {
                [WorkBookingRefusalCodes.DateField] = request.Date,
                [WorkBookingRefusalCodes.SealLevelField] = kind,
            },
            []);
    }

    private async Task<WorkBookingRefusalFinding?> FindPeriodRefusalAsync(
        WorkBookingDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var violations = await _planningPeriodGuard.FindViolationsAsync([(request.ClientId, request.Date)], cancellationToken);
        if (violations.Count == 0)
        {
            return null;
        }

        var violation = violations[0];
        var outsideMembership = violation.Code == WorkBookingRefusalCodes.OutsideMembershipPeriod;
        return new WorkBookingRefusalFinding(
            outsideMembership ? WorkBookingRefusalReason.OutsideMembershipPeriod : WorkBookingRefusalReason.OutsideGroupPeriod,
            violation.Code,
            new WorkBookingChannels(DirectEntry: true, Assistant: true, Grid: true),
            outsideMembership ? WorkBookingRemedy.AdjustMembership : WorkBookingRemedy.AdjustGroupMembership,
            new Dictionary<string, object?>
            {
                [WorkBookingRefusalCodes.DateField] = violation.Date,
                [WorkBookingRefusalCodes.FromField] = violation.From,
                [WorkBookingRefusalCodes.UntilField] = violation.Until,
            },
            []);
    }

    private static WorkBookingRefusalFinding? ToSporadicFinding(WorkBookingRefusal? refusal)
    {
        if (refusal == null)
        {
            return null;
        }

        var dayFull = refusal.Code == WorkWriteConflictCodes.SporadicShiftDayFull;
        return new WorkBookingRefusalFinding(
            dayFull ? WorkBookingRefusalReason.SporadicShiftDayFull : WorkBookingRefusalReason.SporadicShiftRangeExhausted,
            refusal.Code,
            new WorkBookingChannels(DirectEntry: true, Assistant: false, Grid: true),
            WorkBookingRemedy.ChooseOtherDayOrShift,
            refusal.Details,
            []);
    }

    private static WorkBookingRefusalFinding? ToHardBlockingFinding(
        WorkBookingRefusal? directRefusal, PreCommitCheckResult assistantCheck)
    {
        if (directRefusal == null && !assistantCheck.HasHardBlocking)
        {
            return null;
        }

        var conflicts = directRefusal?.Details.TryGetValue(WorkWriteConflictCodes.ConflictsField, out var items) == true
                        && items is IReadOnlyList<WorkConflictItem> directItems
            ? directItems
            : assistantCheck.HardBlockingConflicts
                .Select(c => new WorkConflictItem(c.Comment, c.ClientId, c.Date, c.CommentParams))
                .ToList();

        return new WorkBookingRefusalFinding(
            WorkBookingRefusalReason.HardBlockingConflict,
            WorkWriteConflictCodes.BlockedByConflicts,
            new WorkBookingChannels(DirectEntry: directRefusal != null, Assistant: assistantCheck.HasHardBlocking, Grid: false),
            WorkBookingRemedy.AddQualificationOrRelaxLevel,
            directRefusal?.Details ?? NoDetails,
            conflicts);
    }

    private static WorkBookingRefusalFinding? ToAssistantOnlyFinding(PreCommitCheckResult assistantCheck)
    {
        var hardBlocking = assistantCheck.HardBlockingConflicts.ToHashSet();
        var conflicts = assistantCheck.NewConflicts
            .Where(c => c.Type == ScheduleValidationType.Error && !hardBlocking.Contains(c))
            .Select(c => new WorkConflictItem(c.Comment, c.ClientId, c.Date, c.CommentParams))
            .ToList();
        if (conflicts.Count == 0)
        {
            return null;
        }

        return new WorkBookingRefusalFinding(
            WorkBookingRefusalReason.AssistantOnlyConflict,
            null,
            new WorkBookingChannels(DirectEntry: false, Assistant: true, Grid: false),
            WorkBookingRemedy.AdjustPlanOrEnterDirectly,
            NoDetails,
            conflicts);
    }

    private async Task<IReadOnlyList<WorkBookingRefusalFinding>> FindOccupyingWorksAsync(
        WorkBookingDiagnosisRequest request, CancellationToken cancellationToken)
    {
        var day = request.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var works = (await _workRepository.GetByClientAndDateRangeAsync(request.ClientId, day, day, cancellationToken))
            .Where(w => w.CurrentDate == request.Date && w.AnalyseToken == request.AnalyseToken && w.ParentWorkId == null)
            .ToList();
        if (works.Count == 0)
        {
            return [];
        }

        var visibleShiftIds = await _shiftVisibilityGuard.FilterVisibleAsync(
            works.Select(w => w.ShiftId).Distinct().ToList(), cancellationToken);
        var findings = new List<WorkBookingRefusalFinding>();
        foreach (var occupying in works)
        {
            var visible = visibleShiftIds.Contains(occupying.ShiftId);
            var shiftName = visible
                ? (await _shiftRepository.GetSporadicInfoAsync(occupying.ShiftId, cancellationToken))?.Name
                : null;
            var details = new Dictionary<string, object?> { [WorkBookingRefusalCodes.DateField] = request.Date };
            if (visible)
            {
                details[WorkWriteConflictCodes.ShiftNameField] = shiftName;
                details[WorkBookingRefusalCodes.LockLevelField] = occupying.LockLevel;
            }

            findings.Add(new WorkBookingRefusalFinding(
                WorkBookingRefusalReason.CellOccupiedByWork,
                null,
                new WorkBookingChannels(DirectEntry: false, Assistant: false, Grid: true),
                visible ? RemedyForOccupyingWork(occupying.LockLevel) : WorkBookingRemedy.AskForGroupVisibility,
                details,
                []));
        }

        return findings;
    }

    private async Task<WorkBookingRefusalFinding?> FindOccupyingAbsenceAsync(
        WorkBookingDiagnosisRequest request, bool inScenario, CancellationToken cancellationToken)
    {
        if (inScenario)
        {
            return null;
        }

        var breaks = await _breakRepository.GetByClientAndDateRangeAsync(
            request.ClientId, request.Date, request.Date, cancellationToken);
        if (!breaks.Any(absence => absence.ParentWorkId == null))
        {
            return null;
        }

        return new WorkBookingRefusalFinding(
            WorkBookingRefusalReason.CellOccupiedByAbsence,
            null,
            new WorkBookingChannels(DirectEntry: false, Assistant: false, Grid: true),
            WorkBookingRemedy.RemoveOrMoveExistingEntry,
            new Dictionary<string, object?> { [WorkBookingRefusalCodes.DateField] = request.Date },
            []);
    }

    private static WorkBookingRemedy RemedyForOccupyingWork(WorkLockLevel lockLevel) => lockLevel switch
    {
        WorkLockLevel.Closed => WorkBookingRemedy.ReopenPeriodAdmin,
        WorkLockLevel.Approved => WorkBookingRemedy.RevokeDayApproval,
        WorkLockLevel.Confirmed => WorkBookingRemedy.RemoveConfirmationFirst,
        _ => WorkBookingRemedy.RemoveOrMoveExistingEntry,
    };

    private static WorkBookingDiagnosisOutcome OutcomeOf(IReadOnlyList<WorkBookingRefusalFinding> refusals)
    {
        if (refusals.Any(refusal => refusal.Channels.RefusesAnyWrite))
        {
            return WorkBookingDiagnosisOutcome.Refused;
        }

        return refusals.Count > 0 ? WorkBookingDiagnosisOutcome.GridOnly : WorkBookingDiagnosisOutcome.Bookable;
    }

    private static void AddIfPresent(List<WorkBookingRefusalFinding> refusals, WorkBookingRefusalFinding? finding)
    {
        if (finding != null)
        {
            refusals.Add(finding);
        }
    }
}
