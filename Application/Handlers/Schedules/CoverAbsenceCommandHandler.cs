// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="CoverAbsenceCommand"/>. Orchestrates the disruption flow in one call: create a
/// scenario and clone the real schedule under its token, build an immutable recovery snapshot from the
/// live plan, ask the pure <see cref="IRecoveryEngine"/> for the minimal-perturbation repair, record the
/// absence (Break) for the employee in the scenario, then materialise every reassignment delta (direct
/// covers and swap relocations) as a Replacement WorkChange on the corresponding cloned work (which
/// inherits the scenario token through the WorkChange handler). Locked / uncoverable slots are reported.
/// The proposal is partitioned via <see cref="ICompliancePartitionService"/> (pre-commit guardrail plus
/// the K1 supervisor override); blocked deltas are reported as uncovered, and the non-blocking rule
/// conflicts on the materialised set are surfaced in the outcome for supervised review.
/// An absent employee outside the caller's group visibility is refused exactly like an employee that does
/// not exist, before anything is written; repair options that would write for a replacement or swap partner
/// outside the caller's visibility are dropped and reported as uncovered (no eligible candidate).
/// </summary>
/// <param name="scenarioRepository">Persists the new AnalyseScenario</param>
/// <param name="scenarioService">Clones the real schedule under the scenario token (with the work id map)</param>
/// <param name="scheduleEntriesService">Reads the absent employee's slots to size the absence Break</param>
/// <param name="snapshotBuilder">Builds the immutable recovery snapshot from the live plan</param>
/// <param name="recoveryEngine">The pure, deterministic re-rostering engine</param>
/// <param name="partitionService">Shared accept/block partition incl. the K1 supervisor override</param>
/// <param name="mediator">Dispatches the Break and Replacement-WorkChange commands</param>
/// <param name="unitOfWork">Flushes the scenario + clone before the slots are read</param>
/// <param name="escalationChainService">Starts the messenger call-list for each day the absence leaves a shift needing a human decision (skipped when the command says the planner is handling it interactively)</param>
/// <param name="companyClock">Resolves the company's time zone to DST-safely convert the absent employee's shift start to UTC</param>
/// <param name="clientVisibilityGuard">Decides for which employees the calling user may write</param>
/// <param name="scenarioNameGenerator">Builds the localized, per-group unique scenario name</param>
/// <param name="logger">Logs residual blocking conflicts for supervised review</param>
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules.Recovery;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.ScheduleRecovery.Engine;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Rec = Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class CoverAbsenceCommandHandler : IRequestHandler<CoverAbsenceCommand, CoverAbsenceOutcome>
{
    private const string LockedReason = "locked";
    private const string NoCandidateReason = "no eligible candidate";
    private const string NonCriticalReason = "non-critical";
    private const string BlockedReason = "blocked";
    private const int HoursPerDay = 24;
    private const int MaxAbsenceDays = 31;
    private const decimal DefaultAbsenceHours = 8m;
    private static readonly TimeOnly DayStart = new(0, 0);
    private static readonly TimeOnly DayEnd = new(23, 59);

    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IRecoverySnapshotBuilder _snapshotBuilder;
    private readonly IRecoveryEngine _recoveryEngine;
    private readonly ICompliancePartitionService _partitionService;
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEscalationChainService _escalationChainService;
    private readonly ICompanyClock _companyClock;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;
    private readonly IScenarioNameGenerator _scenarioNameGenerator;
    private readonly ILogger<CoverAbsenceCommandHandler> _logger;

    public CoverAbsenceCommandHandler(
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IScheduleEntriesService scheduleEntriesService,
        IRecoverySnapshotBuilder snapshotBuilder,
        IRecoveryEngine recoveryEngine,
        ICompliancePartitionService partitionService,
        IMediator mediator,
        IUnitOfWork unitOfWork,
        IEscalationChainService escalationChainService,
        ICompanyClock companyClock,
        IClientVisibilityGuard clientVisibilityGuard,
        IScenarioNameGenerator scenarioNameGenerator,
        ILogger<CoverAbsenceCommandHandler> logger)
    {
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _scheduleEntriesService = scheduleEntriesService;
        _snapshotBuilder = snapshotBuilder;
        _recoveryEngine = recoveryEngine;
        _partitionService = partitionService;
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _escalationChainService = escalationChainService;
        _companyClock = companyClock;
        _clientVisibilityGuard = clientVisibilityGuard;
        _scenarioNameGenerator = scenarioNameGenerator;
        _logger = logger;
    }

    public async Task<CoverAbsenceOutcome> Handle(CoverAbsenceCommand request, CancellationToken cancellationToken)
    {
        var clientId = request.ClientId;
        var date = request.Date;
        var untilDate = request.UntilDate ?? request.Date;
        var groupId = request.GroupId;
        var absenceId = request.AbsenceId;

        if (untilDate < date)
        {
            throw new ArgumentException($"UntilDate ({untilDate}) must not be before Date ({date}).");
        }

        var totalDays = untilDate.DayNumber - date.DayNumber + 1;
        if (totalDays > MaxAbsenceDays)
        {
            throw new ArgumentException(
                $"Absence spans {totalDays} days; the maximum is {MaxAbsenceDays}. Split into smaller periods.");
        }

        if (!await _clientVisibilityGuard.IsVisibleAsync(clientId, cancellationToken))
        {
            throw new KeyNotFoundException($"Client with ID {clientId} not found");
        }

        var dates = Enumerable.Range(0, totalDays).Select(offset => date.AddDays(offset)).ToList();

        var token = Guid.NewGuid();
        var name = await _scenarioNameGenerator.GenerateAsync(
            ScenarioNameKind.AbsenceCover, date, untilDate, groupId, request.Language, cancellationToken);
        var scenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = date,
            UntilDate = untilDate,
            Token = token,
            RunGroupId = Guid.NewGuid()
        };
        await _scenarioRepository.Add(scenario);

        var (_, workIdMap) = await _scenarioService.CloneScenarioDataWithMapsAsync(
            groupId, date, untilDate, token, additionalShiftIds: null, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var snapshot = await _snapshotBuilder.BuildAsync(groupId, clientId, dates, cancellationToken);
        var proposal = _recoveryEngine.Repair(
            snapshot, new Rec.AbsenceEvent(clientId, dates), Rec.Ruleset.Default);

        var absenceDays = await RecordAbsencesAsync(clientId, dates, absenceId, groupId, token, cancellationToken);
        if (request.NotifyEscalationRoster)
        {
            await StartEscalationChainsAsync(clientId, groupId, snapshot, absenceDays, cancellationToken);
        }

        var (visibleDeltas, hiddenOptions) = await SplitByAgentVisibilityAsync(
            proposal.Deltas, clientId, cancellationToken);

        var (materializable, blockedOptions, complianceWarnings) = await PartitionDeltasAsync(
            visibleDeltas, clientId, workIdMap, token, request.OverrideBlock, cancellationToken);

        // Memberships only make sense for covers that survived the partition; a blocked option would
        // otherwise leave an orphaned cross-group membership behind.
        var acceptedAgents = materializable.Select(d => d.ToAgentId).ToHashSet();
        await MaterialiseMembershipsAsync(proposal, acceptedAgents, token, cancellationToken);
        await MaterialiseAsync(materializable, workIdMap, cancellationToken);

        var covered = BuildCovered(materializable, clientId, snapshot, workIdMap);
        var uncovered = BuildUncovered(proposal, blockedOptions, hiddenOptions, clientId, snapshot, workIdMap);

        // Computed after the partition: a blocked swap must not be reported as a tier the result reached.
        var highestTier = materializable.Count > 0 ? materializable.Max(d => (int)d.Tier) : 0;
        if (uncovered.Count > 0)
        {
            highestTier = Math.Max(highestTier, (int)Rec.EscalationTier.Uncovered);
        }

        return new CoverAbsenceOutcome(
            scenario.Id, token, name, covered, uncovered, complianceWarnings, highestTier);
    }

    /// <summary>
    /// One query serves both the Break's WorkTime hours and, from the earliest Work slot that day, the
    /// (WorkId, ShiftStartUtc) StartEscalationChainsAsync needs - an UncoveredSlot from the recovery
    /// engine carries no start time, so this is resolved from the live plan instead of the proposal.
    /// </summary>
    private async Task<(decimal Hours, Guid? WorkId, DateTime? ShiftStartUtc)> ResolveAbsenceDaySlotAsync(
        Guid clientId, DateOnly date, Guid groupId, CancellationToken cancellationToken)
    {
        var slots = await _scheduleEntriesService
            .GetScheduleEntriesQuery(date, date, [groupId], null)
            .Where(c => c.EntryType == (int)ScheduleEntryType.Work && c.ClientId == clientId)
            .OrderBy(c => c.StartTime)
            .ToListAsync(cancellationToken);

        if (slots.Count == 0)
        {
            return (DefaultAbsenceHours, null, null);
        }

        var hours = slots.Sum(s => WorkHours(TimeOnly.FromTimeSpan(s.StartTime), TimeOnly.FromTimeSpan(s.EndTime)));
        var earliest = slots[0];
        var companyTimeZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var shiftStartWallClock = date.ToDateTime(TimeOnly.FromTimeSpan(earliest.StartTime));
        var shiftStartUtc = CompanyWallClockToUtcConverter.ConvertToUtc(shiftStartWallClock, companyTimeZone);
        return (hours, earliest.SourceId, shiftStartUtc);
    }

    private async Task MaterialiseAsync(
        IReadOnlyList<Rec.CellDelta> deltas,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        CancellationToken cancellationToken)
    {
        foreach (var delta in deltas)
        {
            // In-group MVP invariant: a snapshot work is backed by exactly one top-level Work
            // (get_schedule_entries returns parent_work_id IS NULL rows; the builder writes [SourceId]).
            if (delta.SourceWorkIds.Count > 1)
            {
                _logger.LogWarning(
                    "Recovery delta for shift {ShiftId} on {Date} is backed by {Count} works; only the first is materialised.",
                    delta.ShiftId, delta.Date, delta.SourceWorkIds.Count);
            }

            var originalWorkId = delta.SourceWorkIds.Count > 0 ? delta.SourceWorkIds[0] : Guid.Empty;
            if (originalWorkId == Guid.Empty || !workIdMap.TryGetValue(originalWorkId, out var clonedWorkId))
            {
                _logger.LogWarning(
                    "Recovery delta for shift {ShiftId} on {Date} has no cloned work to attach to; skipping.",
                    delta.ShiftId, delta.Date);
                continue;
            }

            await _mediator.Send(new PostCommand<WorkChangeResource>(new WorkChangeResource
            {
                WorkId = clonedWorkId,
                Type = WorkChangeType.ReplacementWithin,
                StartTime = TimeOnly.FromDateTime(delta.StartAt),
                EndTime = TimeOnly.FromDateTime(delta.EndAt),
                ChangeTime = delta.Hours,
                ReplaceClientId = delta.ToAgentId,
                Description = RecoveryMarkers.WorkChangeSource
            }), cancellationToken);
        }
    }

    private async Task MaterialiseMembershipsAsync(
        Rec.RecoveryProposal proposal,
        IReadOnlySet<Guid> acceptedAgents,
        Guid token,
        CancellationToken cancellationToken)
    {
        foreach (var membership in proposal.MembershipDeltas)
        {
            if (!acceptedAgents.Contains(membership.AgentId))
            {
                continue;
            }

            await _scenarioService.AddScenarioMembershipAsync(
                token, membership.AgentId, membership.GroupId,
                membership.ValidFrom, membership.ValidUntil, cancellationToken);
        }
    }

    /// <summary>
    /// The engine reasons over a bounded window; the shared partition service sees the full history,
    /// the K1 Block-mode escalation and the supervisor-override path. Only the deltas actually
    /// causing a block are dropped (greedy per violating client); everything else is materialised.
    /// A structural error (collision, missing mandatory qualification) is never overridable.
    /// </summary>
    /// <summary>
    /// Groups the engine's deltas into atomic repair options and partitions those. A swap consists of a
    /// relocation hop and a cover hop; judging them as independent rows made the relocation look like a
    /// double booking and let a swap end up half-applied.
    /// </summary>
    private async Task<(
        IReadOnlyList<Rec.CellDelta> Materializable,
        IReadOnlyList<IReadOnlyList<Rec.CellDelta>> BlockedOptions,
        IReadOnlyList<ScheduleValidationNotificationDto> Warnings)> PartitionDeltasAsync(
        IReadOnlyList<Rec.CellDelta> deltas,
        Guid absentClientId,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        Guid token,
        bool overrideBlockRequested,
        CancellationToken cancellationToken)
    {
        if (deltas.Count == 0)
        {
            return (deltas, [], []);
        }

        var groups = deltas
            .GroupBy(d => d.OptionId)
            .OrderBy(g => g.Key)
            .Select(g => g.ToList())
            .ToList();

        var options = groups
            .Select(group => new PlannedOption(
                group.Select(d => new PlannedWorkRow(
                    d.ToAgentId, d.Date, TimeOnly.FromDateTime(d.StartAt), TimeOnly.FromDateTime(d.EndAt), d.ShiftId))
                    .ToList(),
                group
                    .Where(d => d.FromAgentId != absentClientId && d.SourceWorkIds.Count > 0)
                    .Select(d => new PlannedRemovalRow(
                        d.FromAgentId,
                        d.Date,
                        TimeOnly.FromDateTime(d.StartAt),
                        TimeOnly.FromDateTime(d.EndAt),
                        // The partition runs under the scenario token, so it sees the cloned works.
                        workIdMap.TryGetValue(d.SourceWorkIds[0], out var clonedId) ? clonedId : null))
                    .ToList()))
            .ToList();

        var partition = await _partitionService.PartitionOptionsAsync(
            options, token, overrideBlockRequested, cancellationToken);

        var materializable = partition.AcceptedOptionIndexes.SelectMany(i => groups[i]).ToList();
        var blockedOptions = partition.BlockedOptions
            .Select(b => (IReadOnlyList<Rec.CellDelta>)groups[b.Index])
            .ToList();

        if (blockedOptions.Count > 0)
        {
            _logger.LogWarning(
                "Recovery proposal for scenario {Token} has {Count} blocking conflict(s) after repair; the affected slot(s) are reported as uncovered instead of committed.",
                token, blockedOptions.Count);
        }

        return (materializable, blockedOptions, partition.ReportableConflicts);
    }

    /// <summary>
    /// The candidate pool spans the whole root group, so the engine may pick a replacement or swap partner the
    /// caller cannot see. Every repair option (all hops sharing an OptionId) that touches such an employee is
    /// dropped as a whole, so no swap is half-applied and nothing is written for a hidden employee.
    /// </summary>
    private async Task<(
        IReadOnlyList<Rec.CellDelta> Visible,
        IReadOnlyList<IReadOnlyList<Rec.CellDelta>> HiddenOptions)> SplitByAgentVisibilityAsync(
        IReadOnlyList<Rec.CellDelta> deltas,
        Guid absentClientId,
        CancellationToken cancellationToken)
    {
        if (deltas.Count == 0)
        {
            return (deltas, []);
        }

        var agentIds = deltas
            .SelectMany(d => new[] { d.FromAgentId, d.ToAgentId })
            .Where(id => id != absentClientId)
            .Distinct()
            .ToList();
        var visibleAgents = (await _clientVisibilityGuard.FilterVisibleAsync(agentIds, id => id, cancellationToken))
            .ToHashSet();
        visibleAgents.Add(absentClientId);

        var hiddenOptionIds = deltas
            .Where(d => !visibleAgents.Contains(d.FromAgentId) || !visibleAgents.Contains(d.ToAgentId))
            .Select(d => d.OptionId)
            .ToHashSet();

        if (hiddenOptionIds.Count == 0)
        {
            return (deltas, []);
        }

        var visible = deltas.Where(d => !hiddenOptionIds.Contains(d.OptionId)).ToList();
        var hiddenOptions = deltas
            .Where(d => hiddenOptionIds.Contains(d.OptionId))
            .GroupBy(d => d.OptionId)
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyList<Rec.CellDelta>)g.ToList())
            .ToList();

        return (visible, hiddenOptions);
    }

    private static IReadOnlyList<CoveredSlot> BuildCovered(
        IReadOnlyList<Rec.CellDelta> deltas,
        Guid absentClientId,
        Rec.RecoverySnapshot snapshot,
        IReadOnlyDictionary<Guid, Guid> workIdMap)
    {
        var covered = new List<CoveredSlot>();
        foreach (var delta in deltas)
        {
            if (delta.FromAgentId != absentClientId)
            {
                continue;
            }
            var name = snapshot.FindAgent(delta.ToAgentId)?.DisplayName ?? string.Empty;
            covered.Add(new CoveredSlot(
                delta.ShiftId ?? Guid.Empty,
                delta.Date,
                delta.ToAgentId,
                name,
                (int)delta.Tier,
                ClonedWorkIdOf(delta.SourceWorkIds, workIdMap),
                TimeOnly.FromDateTime(delta.StartAt),
                TimeOnly.FromDateTime(delta.EndAt)));
        }
        return covered;
    }

    private static IReadOnlyList<UncoveredSlot> BuildUncovered(
        Rec.RecoveryProposal proposal,
        IReadOnlyList<IReadOnlyList<Rec.CellDelta>> blockedOptions,
        IReadOnlyList<IReadOnlyList<Rec.CellDelta>> hiddenOptions,
        Guid absentClientId,
        Rec.RecoverySnapshot snapshot,
        IReadOnlyDictionary<Guid, Guid> workIdMap)
    {
        var uncovered = new List<UncoveredSlot>();
        foreach (var slot in proposal.Uncovered)
        {
            var reason = slot.Reason switch
            {
                Rec.RecoveryReasons.Locked => LockedReason,
                Rec.RecoveryReasons.NonCritical => NonCriticalReason,
                _ => NoCandidateReason
            };
            // The engine's uncovered marker carries no times; the absent agent's own work in the snapshot does.
            var work = FindAbsentWork(snapshot, absentClientId, slot.ShiftId, slot.Date);
            uncovered.Add(new UncoveredSlot(
                slot.ShiftId ?? Guid.Empty,
                slot.Date,
                reason,
                ClonedWorkIdOf(slot.SourceWorkIds, workIdMap),
                work is null ? null : TimeOnly.FromDateTime(work.StartAt),
                work is null ? null : TimeOnly.FromDateTime(work.EndAt)));
        }
        foreach (var option in blockedOptions)
        {
            // Report the slot that actually stayed uncovered - the cover hop - not the foreign shift the
            // relocation half would have touched.
            var cover = option.FirstOrDefault(d => d.FromAgentId == absentClientId) ?? option[0];
            uncovered.Add(UncoveredFromDelta(cover, BlockedReason, workIdMap));
        }
        foreach (var option in hiddenOptions)
        {
            var cover = option.FirstOrDefault(d => d.FromAgentId == absentClientId) ?? option[0];
            uncovered.Add(UncoveredFromDelta(cover, NoCandidateReason, workIdMap));
        }
        return uncovered;
    }

    private static UncoveredSlot UncoveredFromDelta(
        Rec.CellDelta cover, string reason, IReadOnlyDictionary<Guid, Guid> workIdMap)
        => new(
            cover.ShiftId ?? Guid.Empty,
            cover.Date,
            reason,
            ClonedWorkIdOf(cover.SourceWorkIds, workIdMap),
            TimeOnly.FromDateTime(cover.StartAt),
            TimeOnly.FromDateTime(cover.EndAt));

    private static Rec.RecoveryWork? FindAbsentWork(
        Rec.RecoverySnapshot snapshot, Guid absentClientId, Guid? shiftId, DateOnly date)
        => snapshot.GetWorks(absentClientId, date).FirstOrDefault(w => w.IsWorking && w.ShiftId == shiftId);

    /// <summary>
    /// The scenario clone of the first underlying work - where the proposal, or the open slot, lives for the
    /// planner. Null when the work was not cloned (e.g. a sealed cross-group work).
    /// </summary>
    private static Guid? ClonedWorkIdOf(IReadOnlyList<Guid> sourceWorkIds, IReadOnlyDictionary<Guid, Guid> workIdMap)
        => sourceWorkIds.Count > 0 && workIdMap.TryGetValue(sourceWorkIds[0], out var clonedId) ? clonedId : null;

    private async Task<IReadOnlyList<(DateOnly Date, Guid? WorkId, DateTime? ShiftStartUtc, Guid? BreakId)>> RecordAbsencesAsync(
        Guid clientId,
        IReadOnlyList<DateOnly> dates,
        Guid absenceId,
        Guid groupId,
        Guid token,
        CancellationToken cancellationToken)
    {
        var breaks = new List<BulkBreakItem>();
        var daySlots = new List<(decimal Hours, Guid? WorkId, DateTime? ShiftStartUtc)>();
        foreach (var day in dates)
        {
            var slot = await ResolveAbsenceDaySlotAsync(clientId, day, groupId, cancellationToken);
            daySlots.Add(slot);
            breaks.Add(new BulkBreakItem
            {
                ClientId = clientId,
                AbsenceId = absenceId,
                CurrentDate = day,
                StartTime = DayStart,
                EndTime = DayEnd,
                WorkTime = slot.Hours,
                AnalyseToken = token
            });
        }

        var response = await _mediator.Send(new BulkAddBreaksCommand(new BulkAddBreaksRequest
        {
            PeriodStart = dates[0],
            PeriodEnd = dates[^1],
            Breaks = breaks
        }), cancellationToken);

        // CreatedIds is appended to in Breaks order and only skips an index on a per-item construction
        // failure (BulkAddBreaksCommandHandler's try/catch around a plain object initializer) - treated
        // here as practically unreachable, so index i is taken to be dates[i]'s Break id.
        var results = new List<(DateOnly, Guid?, DateTime?, Guid?)>();
        for (var i = 0; i < dates.Count; i++)
        {
            var breakId = i < response.CreatedIds.Count ? response.CreatedIds[i] : (Guid?)null;
            results.Add((dates[i], daySlots[i].WorkId, daySlots[i].ShiftStartUtc, breakId));
        }
        return results;
    }

    /// <summary>
    /// One chain per day that still had a Work slot for the absent employee, independent of whether the
    /// recovery engine covered or left that day uncovered: either way a human must review and accept the
    /// scenario, and the escalation chain's job is getting that human's attention (E1). Days without any
    /// resolvable Work slot (WorkId is null) have nothing to escalate against and are skipped.
    /// </summary>
    private async Task StartEscalationChainsAsync(
        Guid clientId,
        Guid groupId,
        Rec.RecoverySnapshot snapshot,
        IReadOnlyList<(DateOnly Date, Guid? WorkId, DateTime? ShiftStartUtc, Guid? BreakId)> absenceDays,
        CancellationToken cancellationToken)
    {
        var absentClientName = snapshot.FindAgent(clientId)?.DisplayName ?? string.Empty;

        foreach (var day in absenceDays)
        {
            if (day.WorkId is null || day.ShiftStartUtc is null)
            {
                continue;
            }

            await _escalationChainService.StartChainAsync(new StartEscalationChainRequest(
                day.WorkId.Value, groupId, clientId, absentClientName, day.ShiftStartUtc.Value, day.BreakId),
                cancellationToken);
        }
    }

    private static decimal WorkHours(TimeOnly start, TimeOnly end)
    {
        var duration = end.ToTimeSpan() - start.ToTimeSpan();
        if (duration.TotalHours <= 0)
        {
            duration = duration.Add(TimeSpan.FromHours(HoursPerDay));
        }
        return (decimal)duration.TotalHours;
    }
}
