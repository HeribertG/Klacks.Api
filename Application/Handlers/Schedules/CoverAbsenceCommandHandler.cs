// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler for <see cref="CoverAbsenceCommand"/>. Orchestrates the disruption flow in one call: create a
/// scenario and clone the real schedule under its token, build an immutable recovery snapshot from the
/// live plan, ask the pure <see cref="IRecoveryEngine"/> for the minimal-perturbation repair, record the
/// absence (Break) for the employee in the scenario, then materialise every reassignment delta (direct
/// covers and swap relocations) through IAbsenceCoverMaterializer, as a move of the cloned work or as a
/// Replacement WorkChange on it. Locked / uncoverable slots are reported.
/// The proposal is partitioned via <see cref="ICompliancePartitionService"/> (pre-commit guardrail plus
/// the K1 supervisor override); blocked deltas are reported as uncovered, and the non-blocking rule
/// conflicts on the materialised set are surfaced in the outcome for supervised review.
/// A group or an absent employee outside the caller's group visibility is refused exactly like one that does
/// not exist, before any scenario, plan clone or absence is written; candidates outside the caller's visibility leave the
/// pool before the repair. An option the partition refuses is excluded and the engine repairs again (a few rounds), and a
/// swap chain that hands a work on, on another day, widens the scenario to that day so the accept applies all of it.
/// All writes of one cover (scenario, plan clone, absence breaks, memberships, replacement WorkChanges, request-book
/// rows) run in a single IUnitOfWork.ExecuteInTransactionAsync, so a failure leaves no half-filled scenario; the
/// messenger call-list starts only after that commit.
/// With an AnalyseToken the cover is built on that scenario instead of the real plan (scenario from scenario): the source
/// must be visible to the caller (a hidden or unknown one answers like a missing one), still active and cover the
/// requested group; the clone, the snapshot and the absence hours then read the source scenario inside its range, and
/// the reported/recorded shift ids are mapped to the real root shift so the request book and accept stay keyed alike.
/// The new scenario then spans the source scenario's range as well (ScenarioSourceRange), so it carries the whole source
/// plan and accepting it never drops the source's changes outside the absence days.
/// </summary>
/// <param name="scenarioRepository">Persists the new AnalyseScenario</param>
/// <param name="scenarioService">Clones the real schedule under the scenario token (with the work id map)</param>
/// <param name="scheduleEntriesService">Reads the absent employee's slots to size the absence Break</param>
/// <param name="snapshotBuilder">Builds the immutable recovery snapshot from the live plan</param>
/// <param name="recoveryEngine">The pure, deterministic re-rostering engine</param>
/// <param name="partitionService">Shared accept/block partition incl. the K1 supervisor override</param>
/// <param name="mediator">Dispatches the Break commands</param>
/// <param name="unitOfWork">Flushes the scenario + clone before the slots are read</param>
/// <param name="escalationChainService">Starts the messenger call-list for each day the absence leaves a shift needing a human decision (skipped when the command says the planner is handling it interactively)</param>
/// <param name="companyClock">Resolves the company's time zone to DST-safely convert the absent employee's shift start to UTC</param>
/// <param name="clientVisibilityGuard">Decides for which employees the calling user may write</param>
/// <param name="groupVisibilityGuard">Decides whether the calling user may plan the requested group at all</param>
/// <param name="scenarioNameGenerator">Builds the localized, per-group unique scenario name</param>
/// <param name="replacementRequestRecorder">Stages one Proposed replacement request row per covered slot (request book)</param>
/// <param name="coverMaterializer">Writes the accepted reassignments into the scenario</param>
/// <param name="logger">Logs residual blocking conflicts for supervised review</param>
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Groups;
using Klacks.Api.Application.Services.Schedules;
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
using Microsoft.Extensions.Logging;
using Rec = Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.Handlers.Schedules;

public sealed class CoverAbsenceCommandHandler : IRequestHandler<CoverAbsenceCommand, CoverAbsenceOutcome>
{
    private const string LockedReason = "locked";
    private const string RecalculationFailedReason = "recalculation failed";
    private const string NoCandidateReason = "no eligible candidate";
    private const string NonCriticalReason = "non-critical";
    private const string BlockedReason = "blocked";
    private const int HoursPerDay = 24;
    private const int MaxAbsenceDays = 31;
    private const int MaxCoverRounds = 4;
    private const decimal DefaultAbsenceHours = 8m;
    private const string GroupNotFoundMessage = "Group with ID {0} not found";
    private const string SourceScenarioNotFoundMessage = "Scenario with token {0} not found";
    private const string SourceScenarioNotActiveMessage =
        "Scenario {0} is no longer active (status {1}); an absence can only be covered in an active scenario.";
    private const string SourceScenarioGroupMessage =
        "Scenario {0} only covers group {1} and its subgroups; group {2} cannot be planned in it.";
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
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IScenarioNameGenerator _scenarioNameGenerator;
    private readonly IReplacementRequestRecorder _replacementRequestRecorder;
    private readonly IAbsenceCoverMaterializer _coverMaterializer;
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
        IGroupVisibilityGuard groupVisibilityGuard,
        IScenarioNameGenerator scenarioNameGenerator,
        IReplacementRequestRecorder replacementRequestRecorder,
        IAbsenceCoverMaterializer coverMaterializer,
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
        _groupVisibilityGuard = groupVisibilityGuard;
        _scenarioNameGenerator = scenarioNameGenerator;
        _replacementRequestRecorder = replacementRequestRecorder;
        _coverMaterializer = coverMaterializer;
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

        if (!await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(GroupNotFoundMessage, groupId));
        }

        if (!await _clientVisibilityGuard.IsVisibleAsync(clientId, cancellationToken))
        {
            throw new KeyNotFoundException($"Client with ID {clientId} not found");
        }

        var source = await ResolveSourceScopeAsync(request.AnalyseToken, groupId, cancellationToken);

        var dates = Enumerable.Range(0, totalDays).Select(offset => date.AddDays(offset)).ToList();

        var name = await _scenarioNameGenerator.GenerateAsync(
            ScenarioNameKind.AbsenceCover, date, untilDate, groupId, request.Language, cancellationToken);

        // One transaction for everything the cover writes - scenario, plan clone, absence breaks, memberships,
        // replacement WorkChanges and the request-book rows: a failure anywhere leaves no half-filled scenario.
        // The messenger call-list is NOT part of it - a sent message cannot be rolled back, so it starts only
        // after the commit, from the snapshot and absence days the committed run produced.
        var run = await _unitOfWork.ExecuteInTransactionAsync(
            () => WriteCoverScenarioAsync(request, name, dates, source, cancellationToken));

        if (request.NotifyEscalationRoster)
        {
            await StartEscalationChainsAsync(clientId, groupId, run.Outcome.ScenarioId, run.Snapshot, run.AbsenceDays, cancellationToken);
        }

        return run.Outcome;
    }

    private async Task<(
        CoverAbsenceOutcome Outcome,
        Rec.RecoverySnapshot Snapshot,
        IReadOnlyList<(DateOnly Date, Guid? WorkId, DateTime? ShiftStartUtc, Guid? BreakId)> AbsenceDays)>
        WriteCoverScenarioAsync(
            CoverAbsenceCommand request,
            string name,
            IReadOnlyList<DateOnly> dates,
            ScenarioSourceScope? source,
            CancellationToken cancellationToken)
    {
        var clientId = request.ClientId;
        var date = request.Date;
        var untilDate = request.UntilDate ?? request.Date;
        var groupId = request.GroupId;
        var absenceId = request.AbsenceId;

        var token = Guid.NewGuid();
        var (scenarioFrom, scenarioUntil) = ScenarioSourceRange.Widen(date, untilDate, source?.FromDate, source?.UntilDate);
        var scenario = new AnalyseScenario
        {
            Name = name,
            GroupId = groupId,
            FromDate = scenarioFrom,
            UntilDate = scenarioUntil,
            Token = token,
            RunGroupId = Guid.NewGuid(),
            SourceToken = source?.Token
        };
        await _scenarioRepository.Add(scenario);

        var (_, workIdMap) = await _scenarioService.CloneScenarioDataWithMapsAsync(
            groupId, scenarioFrom, scenarioUntil, token, additionalShiftIds: null, source?.Token, cancellationToken);
        await _unitOfWork.CompleteAsync();

        var report = await ResolveReportAsync(request, cancellationToken);
        var sourceWorks = await _coverMaterializer.FindSourceWorksAsync(workIdMap, token, report, cancellationToken);
        var writableWorkIds = workIdMap.Keys.Where(workId => !sourceWorks.HeldContainers.Contains(workId)).ToHashSet();
        var snapshot = await NarrowToCallerAsync(
            await _snapshotBuilder.BuildAsync(
                groupId, clientId, dates, source, writableWorkIds, sourceWorks.Movable, sourceWorks.Moved, cancellationToken),
            clientId,
            cancellationToken);

        var absenceDays = await RecordAbsencesAsync(clientId, dates, absenceId, groupId, token, source, cancellationToken);

        var (proposal, materializable, blockedOptions, complianceWarnings, earlierBlockedSlots) = await PlanCoverAsync(
            snapshot, clientId, dates, workIdMap, token, request.OverrideBlock, cancellationToken);
        var rootShiftIds = await ResolveRootShiftIdsAsync(proposal, source, cancellationToken);

        // Memberships only make sense for covers that survived the partition; a blocked option would
        // otherwise leave an orphaned cross-group membership behind.
        var acceptedAgents = materializable.Select(d => d.ToAgentId).ToHashSet();
        await MaterialiseMembershipsAsync(proposal, acceptedAgents, token, cancellationToken);
        var materialised = await _coverMaterializer.MaterialiseAsync(
            new CoverMaterialisation(materializable, workIdMap, token, report), cancellationToken);
        var written = materializable.Except(materialised.HeldCovers).ToList();
        WidenToTouchedDays(scenario, written);

        var covered = await _replacementRequestRecorder.RecordProposalsAsync(
            new ReplacementProposalContext(clientId, groupId, absenceId, token, request.Source, request.ReportedAtUtc),
            BuildCovered(written, clientId, snapshot, workIdMap, rootShiftIds, materialised.MovedWorkIds),
            cancellationToken);
        await _unitOfWork.CompleteAsync();

        var uncovered = BuildUncovered(proposal, blockedOptions, earlierBlockedSlots, clientId, snapshot, workIdMap, rootShiftIds)
            .Concat(materialised.HeldCovers
                .GroupBy(held => held.OptionId)
                .Select(option => option.FirstOrDefault(held => held.FromAgentId == clientId) ?? option.First())
                .Select(held => UncoveredFromDelta(held, RecalculationFailedReason, workIdMap, rootShiftIds)))
            .ToList();

        // Computed after the partition: a blocked swap must not be reported as a tier the result reached.
        var highestTier = (int)HighestTierOf(written, uncovered.Count > 0);

        // The engine's rhythm value is only true for the result when every proposed cover was materialised.
        int? harmonyDelta = written.Count == proposal.Deltas.Count ? proposal.HarmonyDelta : null;

        var outcome = new CoverAbsenceOutcome(
            scenario.Id, token, name, covered, uncovered,
            [.. complianceWarnings, .. RelocationWarnings(materialised.RelocationsKeepingExtras, clientId, snapshot)],
            highestTier, harmonyDelta);
        return (outcome, snapshot, absenceDays);
    }

    /// <summary>
    /// The scenario the cover builds on, or null for the real plan. A hidden scenario is refused exactly like an unknown
    /// one; a decided one and a group outside the scenario's group hierarchy are refused before anything is written,
    /// because the scenario holds no rows to copy for them.
    /// </summary>
    private async Task<ScenarioSourceScope?> ResolveSourceScopeAsync(
        Guid? analyseToken, Guid groupId, CancellationToken cancellationToken)
    {
        if (analyseToken is not { } token)
        {
            return null;
        }

        var scenario = await _scenarioRepository.GetByTokenAsync(token, cancellationToken);
        if (!await _groupVisibilityGuard.IsScenarioRowVisibleAsync(scenario, cancellationToken))
        {
            throw new KeyNotFoundException(string.Format(SourceScenarioNotFoundMessage, token));
        }

        if (scenario!.Status != AnalyseScenarioStatus.Active)
        {
            throw new ScenarioNotActiveException(
                string.Format(SourceScenarioNotActiveMessage, scenario.Id, scenario.Status));
        }

        if (scenario.GroupId is { } sourceGroupId
            && !(await _scenarioService.GetGroupHierarchyIdsAsync(sourceGroupId, cancellationToken)).Contains(groupId))
        {
            throw new ConflictException(string.Format(SourceScenarioGroupMessage, scenario.Id, sourceGroupId, groupId));
        }

        var ownedShiftIds = await _scenarioService.GetScenarioShiftIdsWithRootsAsync(scenario.Token, cancellationToken);
        return new ScenarioSourceScope(scenario.Token, scenario.FromDate, scenario.UntilDate, scenario.GroupId, ownedShiftIds);
    }

    /// <summary>
    /// On a source scenario the engine works with that scenario's clone shift ids; the request book, the candidate search
    /// and accept are keyed by the real root shift, exactly as on the real plan. Empty for the real plan.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, Guid>> ResolveRootShiftIdsAsync(
        Rec.RecoveryProposal proposal, ScenarioSourceScope? source, CancellationToken cancellationToken)
    {
        if (source is null)
        {
            return new Dictionary<Guid, Guid>();
        }

        var shiftIds = proposal.Deltas.Select(d => d.ShiftId)
            .Concat(proposal.Uncovered.Select(u => u.ShiftId))
            .OfType<Guid>()
            .Distinct()
            .ToList();
        return await _scenarioService.ResolveRootShiftIdsAsync(shiftIds, cancellationToken);
    }

    private static Guid RootShiftIdOf(Guid? shiftId, IReadOnlyDictionary<Guid, Guid> rootShiftIds)
        => shiftId is { } id ? rootShiftIds.GetValueOrDefault(id, id) : Guid.Empty;

    /// <summary>
    /// One query serves both the Break's WorkTime hours and, from the earliest Work slot that day, the
    /// (WorkId, ShiftStartUtc) StartEscalationChainsAsync needs - an UncoveredSlot from the recovery
    /// engine carries no start time, so this is resolved from the live plan instead of the proposal.
    /// </summary>
    private async Task<(decimal Hours, Guid? WorkId, DateTime? ShiftStartUtc)> ResolveAbsenceDaySlotAsync(
        Guid clientId, DateOnly date, Guid groupId, ScenarioSourceScope? source, CancellationToken cancellationToken)
    {
        var readToken = source is not null && source.OwnsDate(date) ? source.Token : (Guid?)null;
        var slots = await _scheduleEntriesService.GetScheduleEntriesAsync(
            new ScheduleEntriesCriteria(date, date, [groupId], readToken)
            {
                ClientIds = [clientId],
                EntryType = ScheduleEntryType.Work,
                OrderByStartTime = true,
            },
            cancellationToken);

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
                    d.ToAgentId, DateOnly.FromDateTime(d.StartAt), TimeOnly.FromDateTime(d.StartAt), TimeOnly.FromDateTime(d.EndAt), d.ShiftId,
                    d.LeadHours, d.TrailHours, d.ExtraPaidHours))
                    .ToList(),
                group
                    .Where(d => d.FromAgentId != absentClientId && d.SourceWorkIds.Count > 0)
                    .Select(d => new PlannedRemovalRow(
                        d.FromAgentId,
                        d.Date,
                        TimeOnly.FromDateTime(d.StartAt),
                        TimeOnly.FromDateTime(d.EndAt),
                        // The partition runs under the scenario token, so it sees the cloned works.
                        workIdMap.TryGetValue(d.SourceWorkIds[0], out var clonedId) ? clonedId : null,
                        d.MovesAsWhole ? d.ExtraPaidHours : 0m))
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
    /// The candidate pool spans the whole root group, so it can hold employees the caller cannot see. They are taken
    /// out of the pool before the repair, so the engine picks the next visible candidate instead of proposing a cover
    /// that would have to be dropped. The first movable day is today in the company time zone: a swap chain never
    /// takes a work of a day that already passed from its holder.
    /// </summary>
    private async Task<Rec.RecoverySnapshot> NarrowToCallerAsync(
        Rec.RecoverySnapshot snapshot, Guid absentClientId, CancellationToken cancellationToken)
    {
        var candidateIds = snapshot.Agents.Select(a => a.Id).Where(id => id != absentClientId).ToList();
        var visible = (await _clientVisibilityGuard.FilterVisibleAsync(candidateIds, id => id, cancellationToken))
            .ToHashSet();
        var hidden = candidateIds.Where(id => !visible.Contains(id)).ToList();
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);
        return snapshot.Excluding(hidden, []).WithFirstMovableDay(today);
    }

    /// <summary>
    /// Repairs and checks the proposal against the compliance partition. The engine reasons over a bounded window and
    /// a subset of the rules, so the partition can refuse an option the engine found legal; the engine then repairs
    /// again without the assignments of the refused options, up to <see cref="MaxCoverRounds"/> rounds, instead of
    /// leaving the slot open while a second candidate would have passed. The last round is the result. A slot that
    /// ends open after an earlier round had a refused cover for it is remembered, so it is still reported as blocked.
    /// </summary>
    private async Task<(
        Rec.RecoveryProposal Proposal,
        IReadOnlyList<Rec.CellDelta> Materializable,
        IReadOnlyList<IReadOnlyList<Rec.CellDelta>> BlockedOptions,
        IReadOnlyList<ScheduleValidationNotificationDto> Warnings,
        IReadOnlySet<(Guid? ShiftId, DateOnly Date, Guid WorkId)> EarlierBlockedSlots)> PlanCoverAsync(
        Rec.RecoverySnapshot snapshot,
        Guid absentClientId,
        IReadOnlyList<DateOnly> dates,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        Guid token,
        bool overrideBlockRequested,
        CancellationToken cancellationToken)
    {
        var absence = new Rec.AbsenceEvent(absentClientId, dates);
        var refused = new HashSet<Rec.IneligibleKey>();
        var earlierBlockedSlots = new HashSet<(Guid? ShiftId, DateOnly Date, Guid WorkId)>();
        for (var round = 1; ; round++)
        {
            var proposal = _recoveryEngine.Repair(snapshot.Excluding([], refused), absence, Rec.Ruleset.Default);
            var (materializable, blockedOptions, warnings) = await PartitionDeltasAsync(
                proposal.Deltas, absentClientId, workIdMap, token, overrideBlockRequested, cancellationToken);

            var known = refused.Count;
            foreach (var delta in blockedOptions.SelectMany(option => option))
            {
                if (delta.ShiftId is { } shiftId)
                {
                    refused.Add(new Rec.IneligibleKey(delta.ToAgentId, shiftId, delta.Date));
                }
            }

            if (refused.Count == known || round >= MaxCoverRounds)
            {
                return (proposal, materializable, blockedOptions, warnings, earlierBlockedSlots);
            }

            foreach (var option in blockedOptions)
            {
                var cover = CoverOf(option, absentClientId);
                earlierBlockedSlots.Add((cover.ShiftId, cover.Date, cover.SourceWorkIds.FirstOrDefault()));
            }
        }
    }

    /// <summary>The cover hop of an option - the slot of the absent employee - or its first hop when there is none.</summary>
    private static Rec.CellDelta CoverOf(IReadOnlyList<Rec.CellDelta> option, Guid absentClientId)
        => option.FirstOrDefault(d => d.FromAgentId == absentClientId) ?? option[0];

    /// <summary>
    /// The accept replaces the real plan on the days of the scenario only. A swap chain can hand a work on, on another
    /// day than the absence; that day must lie inside the scenario, or the accept would apply only half of the chain.
    /// The clone already holds those days (it reads further than the relief reach of the engine).
    /// </summary>
    private static void WidenToTouchedDays(AnalyseScenario scenario, IReadOnlyList<Rec.CellDelta> written)
    {
        foreach (var delta in written)
        {
            if (delta.Date < scenario.FromDate)
            {
                scenario.FromDate = delta.Date;
            }
            if (delta.Date > scenario.UntilDate)
            {
                scenario.UntilDate = delta.Date;
            }
        }
    }

    /// <summary>
    /// The tier furthest along the escalation order among the accepted deltas, or Uncovered when a slot
    /// stayed open. Compared by <see cref="Rec.EscalationTierSeverity"/>, never by the append-only enum value.
    /// </summary>
    private static Rec.EscalationTier HighestTierOf(IReadOnlyList<Rec.CellDelta> materializable, bool anyUncovered)
        => Rec.EscalationTierSeverity.Highest(materializable.Select(d => d.Tier), anyUncovered);

    /// <summary>
    /// How the absence reached the run: its path and the report instant, a missing or future one read as now.
    /// </summary>
    private async Task<CoverReport> ResolveReportAsync(CoverAbsenceCommand request, CancellationToken cancellationToken)
    {
        var nowUtc = (await _companyClock.GetNowAsync(cancellationToken)).UtcDateTime;
        var reportedAtUtc = request.ReportedAtUtc is { } reported && reported < nowUtc ? reported : nowUtc;
        return new CoverReport(request.Source, reportedAtUtc);
    }

    /// <summary>
    /// A swap relocation that stayed a replacement against the plan: the cap check counted its extras as gone, they stay.
    /// </summary>
    private static IEnumerable<ScheduleValidationNotificationDto> RelocationWarnings(
        IReadOnlyList<Rec.CellDelta> relocationsKeepingExtras, Guid absentClientId, Rec.RecoverySnapshot snapshot)
        => relocationsKeepingExtras
            .Where(delta => delta.FromAgentId != absentClientId)
            .Select(delta => new ScheduleValidationNotificationDto
            {
                Type = ScheduleValidationType.Warning,
                ClientId = delta.FromAgentId,
                ClientName = snapshot.FindAgent(delta.FromAgentId)?.DisplayName ?? string.Empty,
                Date = delta.Date,
                Comment = ScheduleValidationKeys.CoverRelocationKeepsExtras
            });

    private static IReadOnlyList<CoveredSlot> BuildCovered(
        IReadOnlyList<Rec.CellDelta> deltas,
        Guid absentClientId,
        Rec.RecoverySnapshot snapshot,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        IReadOnlyDictionary<Guid, Guid> rootShiftIds,
        IReadOnlySet<Guid> movedWorkIds)
    {
        var covered = new List<CoveredSlot>();
        foreach (var delta in deltas)
        {
            if (delta.FromAgentId != absentClientId)
            {
                continue;
            }
            var name = NameOf(snapshot, delta.ToAgentId);
            var clonedWorkId = ClonedWorkIdOf(delta.SourceWorkIds, workIdMap);
            // The engine lists the hops of a chain deepest first; reversed they read from the cover outwards.
            var relocations = deltas
                .Where(d => d.OptionId == delta.OptionId && d.FromAgentId != absentClientId)
                .Reverse()
                .Select(d => new CoverRelocation(
                    RootShiftIdOf(d.ShiftId, rootShiftIds),
                    d.Date,
                    d.FromAgentId,
                    NameOf(snapshot, d.FromAgentId),
                    d.ToAgentId,
                    NameOf(snapshot, d.ToAgentId),
                    TimeOnly.FromDateTime(d.StartAt),
                    TimeOnly.FromDateTime(d.EndAt),
                    (int)d.Tier))
                .ToList();
            covered.Add(new CoveredSlot(
                RootShiftIdOf(delta.ShiftId, rootShiftIds),
                delta.Date,
                delta.ToAgentId,
                name,
                (int)delta.Tier,
                clonedWorkId,
                TimeOnly.FromDateTime(delta.StartAt),
                TimeOnly.FromDateTime(delta.EndAt),
                Kind: clonedWorkId is { } workId && movedWorkIds.Contains(workId)
                    ? ReplacementRequestKind.Move
                    : ReplacementRequestKind.Replacement,
                Relocations: relocations));
        }
        return covered;
    }

    private static string NameOf(Rec.RecoverySnapshot snapshot, Guid agentId)
        => snapshot.FindAgent(agentId)?.DisplayName ?? string.Empty;

    private static IReadOnlyList<UncoveredSlot> BuildUncovered(
        Rec.RecoveryProposal proposal,
        IReadOnlyList<IReadOnlyList<Rec.CellDelta>> blockedOptions,
        IReadOnlySet<(Guid? ShiftId, DateOnly Date, Guid WorkId)> earlierBlockedSlots,
        Guid absentClientId,
        Rec.RecoverySnapshot snapshot,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        IReadOnlyDictionary<Guid, Guid> rootShiftIds)
    {
        var uncovered = new List<UncoveredSlot>();
        foreach (var slot in proposal.Uncovered)
        {
            // A slot no later round could cover, although an earlier round had a cover the partition refused,
            // stays reported as blocked: the planner can still override that block.
            var reason = slot.Reason switch
            {
                Rec.RecoveryReasons.Locked => LockedReason,
                Rec.RecoveryReasons.NonCritical => NonCriticalReason,
                _ => earlierBlockedSlots.Contains((slot.ShiftId, slot.Date, slot.SourceWorkIds.FirstOrDefault()))
                    ? BlockedReason
                    : NoCandidateReason
            };
            // The engine's uncovered marker carries no times; the absent agent's own work in the snapshot does.
            var work = FindAbsentWork(snapshot, absentClientId, slot.ShiftId, slot.Date);
            uncovered.Add(new UncoveredSlot(
                RootShiftIdOf(slot.ShiftId, rootShiftIds),
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
            uncovered.Add(UncoveredFromDelta(CoverOf(option, absentClientId), BlockedReason, workIdMap, rootShiftIds));
        }
        return uncovered;
    }

    private static UncoveredSlot UncoveredFromDelta(
        Rec.CellDelta cover, string reason, IReadOnlyDictionary<Guid, Guid> workIdMap, IReadOnlyDictionary<Guid, Guid> rootShiftIds)
        => new(
            RootShiftIdOf(cover.ShiftId, rootShiftIds),
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
        ScenarioSourceScope? source,
        CancellationToken cancellationToken)
    {
        var breaks = new List<BulkBreakItem>();
        var daySlots = new List<(decimal Hours, Guid? WorkId, DateTime? ShiftStartUtc)>();
        foreach (var day in dates)
        {
            var slot = await ResolveAbsenceDaySlotAsync(clientId, day, groupId, source, cancellationToken);
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
        }, RemoveTakenWorks: false), cancellationToken);

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
        Guid scenarioId,
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
                day.WorkId.Value, groupId, clientId, absentClientName, day.ShiftStartUtc.Value, day.BreakId, scenarioId),
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
