// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.DTOs.Schedules.Wizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardApplyService"/>. Reads the cached scenario,
/// partitions the non-locked tokens through the shared compliance guardrail and materialises the
/// accepted ones as a <see cref="BulkAddWorksCommand"/> dispatched via mediator. Routing through
/// the existing Work-Create pipeline ensures PeriodHours, Collision-Pipeline, Notifications and
/// Container-Expansion side-effects fire correctly.
/// A scenario apply of a run on a source scenario builds the new scenario on that scenario (scenario from scenario): the
/// clone copies the source scenario and maps its shift ids to the new clones; a token whose shift has no clone is skipped
/// and reported as a skipped placement instead of being written onto another scenario's shift, where it would stay
/// invisible. The new scenario
/// spans the source scenario's range as well (ScenarioSourceRange), so accepting it never drops the source plan, and
/// without an own group it inherits the source scenario's group.
/// A placement on a day outside its employee's membership or group period (the grid's locked cells) is not written but
/// reported as skipped; the rest of the run is applied. The group period is that of the scenario's group when it has one,
/// otherwise the request's group context; the bulk write itself then skips its own period check.
/// </summary>
/// <param name="resultCache">Cache populated by the wizard runner</param>
/// <param name="mediator">Mediator used to dispatch BulkAddWorksCommand</param>
/// <param name="scenarioRepository">Repository for persisting new AnalyseScenario entities</param>
/// <param name="scenarioService">Service for cloning schedule data into the new scenario</param>
/// <param name="unitOfWork">Unit of work for flushing EF changes</param>
/// <param name="softeningRepository">Repository persisting per-cell softening escalations</param>
/// <param name="captureRepository">Repository persisting the run-protocol capture for the preference-learner</param>
/// <param name="partitionService">Shared accept/block compliance partition incl. the K1 supervisor override</param>
/// <param name="companyClock">Resolves the company's current calendar date when the cached scenario has no tokens</param>
/// <param name="scenarioNameGenerator">Builds the localized, per-group unique scenario name</param>
/// <param name="planningPeriodGuard">Names the placements outside their employee's membership or group period; they are
/// reported as skipped instead of written (the planner models the membership per day, but not the group period)</param>
/// <param name="logger">Logger used for best-effort capture warnings</param>
public sealed class WizardApplyService : IWizardApplyService
{
    private const string ShiftNotInScenarioReasonKey = "schedule.error-list.shift-not-in-scenario";
    private const string OutsidePlanningPeriodReasonKey = "schedule.error-list.outside-planning-period";

    private readonly WizardResultCache _resultCache;
    private readonly IMediator _mediator;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWorkSofteningRepository _softeningRepository;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly ICompliancePartitionService _partitionService;
    private readonly DataBaseContext _context;
    private readonly IScheduleTimelineService _timelineService;
    private readonly ICompanyClock _companyClock;
    private readonly IScenarioNameGenerator _scenarioNameGenerator;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;
    private readonly IClientPlanningPeriodGuard _planningPeriodGuard;
    private readonly ILogger<WizardApplyService> _logger;

    public WizardApplyService(
        WizardResultCache resultCache,
        IMediator mediator,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        IWorkSofteningRepository softeningRepository,
        IWizardRunCaptureRepository captureRepository,
        ICompliancePartitionService partitionService,
        DataBaseContext context,
        IScheduleTimelineService timelineService,
        ICompanyClock companyClock,
        IScenarioNameGenerator scenarioNameGenerator,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard,
        IClientPlanningPeriodGuard planningPeriodGuard,
        ILogger<WizardApplyService> logger)
    {
        _planningPeriodGuard = planningPeriodGuard;
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
        _resultCache = resultCache;
        _mediator = mediator;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _softeningRepository = softeningRepository;
        _captureRepository = captureRepository;
        _partitionService = partitionService;
        _context = context;
        _timelineService = timelineService;
        _companyClock = companyClock;
        _scenarioNameGenerator = scenarioNameGenerator;
        _logger = logger;
    }

    public async Task<WizardApplyOutcome> ApplyAsync(Guid jobId, bool overrideBlock, CancellationToken ct)
    {
        // Taking the entry out atomically is what stops a double apply from materialising the same
        // scenario twice; every path that does not consume it puts it back.
        if (!_resultCache.TryTake(jobId, out var scenario, out var analyseToken, out var escalations, out var subScoreJson, out var stage0Violations) || scenario is null)
        {
            throw new InvalidOperationException($"No cached wizard result for job id {jobId}.");
        }

        try
        {
            var items = scenario.Tokens
                .Where(t => !t.IsLocked)
                .Select(t => BuildBulkItem(t, analyseToken))
                .ToList();

            if (items.Count == 0)
            {
                return new WizardApplyOutcome([], [], [], OverrideApplied: false);
            }

            // The direct path writes into the world the wizard ran on: the source scenario when the run
            // carried an analyse token, the real plan otherwise. The partition must check that same world.
            var rows = items
                .Select(i => new PlannedWorkRow(i.ClientId, i.CurrentDate, i.StartTime, i.EndTime, i.ShiftId))
                .ToList();
            var partition = await _partitionService.PartitionAsync(rows, analyseToken, overrideBlock, ct);
            var outsidePeriod = await FindOutsidePeriodIndexesAsync(rows, partition.AcceptedIndexes, groupId: null, jobId, ct);

            var acceptedItems = partition.AcceptedIndexes.Except(outsidePeriod).Select(i => items[i]).ToList();
            var skipped = BuildSkippedPlacements(rows, partition.BlockedIndexes, outsidePeriodIndexes: outsidePeriod);

            if (acceptedItems.Count == 0)
            {
                // Everything was blocked: put the cached result back so a supervisor can retry the
                // same jobId with an authorised override instead of re-running the whole GA.
                _resultCache.Store(jobId, scenario, analyseToken, escalations, subScoreJson, stage0Violations);
                return new WizardApplyOutcome([], partition.ReportableConflicts, skipped, partition.OverrideApplied);
            }

            var periodStart = acceptedItems.Min(i => i.CurrentDate);
            var periodEnd = acceptedItems.Max(i => i.CurrentDate);

            // Works and their escalations must land together: a failure between the two used to leave the
            // plan half-written with no way to tell which softenings the operator actually accepted.
            var createdIds = await _unitOfWork.ExecuteInTransactionAsync<IReadOnlyList<Guid>>(async () =>
            {
                // A transient retry replays this delegate; without clearing, the first attempt's entities
                // would still be tracked and inserted a second time.
                _context.ChangeTracker.Clear();

                var bulkResponse = await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
                {
                    Works = acceptedItems,
                    PeriodStart = periodStart,
                    PeriodEnd = periodEnd,
                }, PlanningPeriodCheck.SkipForPlannerOutput));

                await PersistEscalationsAsync(acceptedItems, escalations, analyseToken, periodStart, periodEnd, ct);

                return bulkResponse.CreatedIds;
            });

            // The bulk handler queues its range check on a background worker with its own context, which
            // inside the transaction would read the pre-commit state. Re-queueing after the commit is
            // idempotent and heals that.
            _timelineService.QueueRangeCheck(periodStart, periodEnd, analyseToken);

            // Direct-apply is the most common acceptance case: the tokens go straight into the real plan without
            // a scenario. ChurnAtApply stays null (first-fill measures nothing); GroupId is unknown on this path.
            await CaptureRunAsync(
                jobId,
                WizardApplyKind.Direct,
                scenarioId: null,
                runGroupId: null,
                groupId: null,
                periodStart,
                periodEnd,
                subScoreJson,
                stage0Violations,
                createdIds,
                ct);

            return new WizardApplyOutcome(createdIds, partition.ReportableConflicts, skipped, partition.OverrideApplied);
        }
        catch
        {
            _resultCache.Store(jobId, scenario, analyseToken, escalations, subScoreJson, stage0Violations);
            throw;
        }
    }

    public async Task<(AnalyseScenarioResource Scenario, WizardApplyOutcome Outcome)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        bool overrideBlock,
        CancellationToken ct,
        ScenarioNameKind? nameKind = null,
        string? language = null)
    {
        if (!_resultCache.TryTake(jobId, out var cachedScenario, out var sourceAnalyseToken, out var escalations, out var subScoreJson, out var stage0Violations) || cachedScenario is null)
        {
            throw new InvalidOperationException($"No cached wizard result for job id {jobId}.");
        }

        try
        {
            var plan = await BuildScenarioPlanAsync(cachedScenario, sourceAnalyseToken, groupId, nameKind, language, ct);

            // Scenario row, clone, slot cleanup and the planner works must land together - a failure in
            // between used to leave a scenario whose schedule was half-written.
            var materialised = await _unitOfWork.ExecuteInTransactionAsync(
                () => MaterialiseScenarioAsync(plan, escalations, jobId, overrideBlock, ct));

            // Range checks run on a background context that would read the pre-commit state from inside
            // the transaction; re-queueing after the commit is idempotent and heals that.
            _timelineService.QueueRangeCheck(plan.ScenarioFrom, plan.ScenarioUntil, plan.Token);

            await CaptureRunAsync(
                jobId,
                WizardApplyKind.Scenario,
                scenarioId: materialised.Scenario.Id,
                runGroupId: plan.RunGroupId,
                groupId: plan.GroupId,
                plan.PeriodFrom,
                plan.PeriodUntil,
                subScoreJson,
                stage0Violations,
                materialised.Ids,
                ct);

            return (
                AppliedScenarioBuilder.ToAppliedResource(materialised.Scenario),
                new WizardApplyOutcome(materialised.Ids, materialised.Partition.ReportableConflicts, materialised.Skipped, materialised.Partition.OverrideApplied));
        }
        catch
        {
            _resultCache.Store(jobId, cachedScenario, sourceAnalyseToken, escalations, subScoreJson, stage0Violations);
            throw;
        }
    }

    private async Task<WizardScenarioApplyPlan> BuildScenarioPlanAsync(
        CoreScenario cachedScenario,
        Guid? sourceAnalyseToken,
        Guid? requestedGroupId,
        ScenarioNameKind? nameKind,
        string? language,
        CancellationToken ct)
    {
        var sourceScenario = await LoadSourceScenarioAsync(sourceAnalyseToken, ct);
        var runGroupId = sourceScenario?.RunGroupId ?? Guid.NewGuid();
        var groupId = ScenarioGroupInheritance.Resolve(requestedGroupId, sourceScenario);

        var items = cachedScenario.Tokens
            .Where(t => !t.IsLocked)
            .ToList();

        var periodFrom = items.Count > 0
            ? items.Min(t => t.Date)
            : await _companyClock.GetTodayDateAsync(ct);
        var periodUntil = items.Count > 0
            ? items.Max(t => t.Date)
            : periodFrom;

        var name = await _scenarioNameGenerator.GenerateAsync(
            nameKind ?? ScenarioNameKind.Plan, periodFrom, periodUntil, groupId, language, ct);
        var (scenarioFrom, scenarioUntil) = ScenarioSourceRange.Widen(
            periodFrom, periodUntil, sourceScenario?.FromDate, sourceScenario?.UntilDate);

        return new WizardScenarioApplyPlan(
            Items: items,
            GroupId: groupId,
            Name: name,
            Token: Guid.NewGuid(),
            RunGroupId: runGroupId,
            SourceAnalyseToken: sourceAnalyseToken,
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            ScenarioFrom: scenarioFrom,
            ScenarioUntil: scenarioUntil);
    }

    private async Task<(IReadOnlyList<Guid> Ids, CompliancePartitionResult Partition, IReadOnlyList<SkippedPlacementDto> Skipped, AnalyseScenario Scenario)> MaterialiseScenarioAsync(
        WizardScenarioApplyPlan plan,
        IReadOnlyList<WizardEscalationDto> escalations,
        Guid jobId,
        bool overrideBlock,
        CancellationToken ct)
    {
        // A transient retry replays this delegate; without clearing, the first attempt's entities
        // would still be tracked and inserted a second time.
        _context.ChangeTracker.Clear();

        var (analyseScenario, shiftIdMap) = await CreateScenarioWithCloneAsync(plan, ct);
        var displacedWorkIds = await SoftDeleteDisplacedWorksAsync(plan, shiftIdMap, ct);
        var (partition, acceptedTokens, skipped) = await PartitionPlacementsAsync(plan, shiftIdMap, overrideBlock, jobId, ct);

        var bulkItems = acceptedTokens
            .Select(t => BuildBulkItem(t, plan.Token, shiftIdMap))
            .ToList();
        var createdIds = await AddPlannerWorksAsync(bulkItems, plan);

        await ResolveDisplacedWorksAsync(plan, displacedWorkIds, createdIds, jobId, ct);
        await PersistEscalationsAsync(bulkItems, escalations, plan.Token, plan.PeriodFrom, plan.PeriodUntil, ct);

        return (createdIds, partition, skipped, analyseScenario);
    }

    private async Task<(AnalyseScenario Scenario, Dictionary<Guid, Guid> ShiftIdMap)> CreateScenarioWithCloneAsync(
        WizardScenarioApplyPlan plan,
        CancellationToken ct)
    {
        var analyseScenario = AppliedScenarioBuilder.Create(
            name: plan.Name,
            groupId: plan.GroupId,
            fromDate: plan.ScenarioFrom,
            untilDate: plan.ScenarioUntil,
            token: plan.Token,
            runGroupId: plan.RunGroupId,
            sourceToken: plan.SourceAnalyseToken);

        await _scenarioRepository.Add(analyseScenario);
        var shiftIdMap = await _scenarioService.CloneScenarioDataAsync(
            plan.GroupId, plan.ScenarioFrom, plan.ScenarioUntil, plan.Token, additionalShiftIds: null, plan.SourceAnalyseToken, ct);
        await _unitOfWork.CompleteAsync();

        return (analyseScenario, shiftIdMap);
    }

    // The planner REPLACES the incumbent on exactly the (shift, date) slots it fills. Remove the cloned
    // movable works on those slots before materialising the planner works, so an accepted plan does not
    // double-book them (grill H2). Slots the planner did not plan keep their cloned work (the run can
    // cover a narrower agent/shift set than the group-scoped clone); locks/breaks are always preserved.
    private async Task<IReadOnlyList<Guid>> SoftDeleteDisplacedWorksAsync(
        WizardScenarioApplyPlan plan,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        CancellationToken ct)
    {
        var plannedSlots = plan.Items
            .Select(t => (ShiftId: shiftIdMap.TryGetValue(t.ShiftRefId, out var mapped) ? mapped : t.ShiftRefId, t.Date))
            .ToHashSet();
        var displacedWorkIds = await _scenarioService.SoftDeleteClonedWorksOnSlotsAsync(plan.Token, plan.PeriodFrom, plan.PeriodUntil, plannedSlots, ct);
        await _unitOfWork.CompleteAsync();

        return displacedWorkIds;
    }

    // Partition AFTER the slot soft-delete has been flushed: the guardrail reads the DB via
    // AsNoTracking under the NEW scenario token, so it must see the clone world exactly as the
    // planner works will land in it (unflushed changes would make the check silently pass or
    // report phantom collisions with the removed incumbents).
    private async Task<(CompliancePartitionResult Partition, List<CoreToken> AcceptedTokens, List<SkippedPlacementDto> Skipped)> PartitionPlacementsAsync(
        WizardScenarioApplyPlan plan,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        bool overrideBlock,
        Guid jobId,
        CancellationToken ct)
    {
        var items = plan.Items;
        var rows = items
            .Select(t => new PlannedWorkRow(
                Guid.Parse(t.AgentId),
                t.Date,
                TimeOnly.FromDateTime(t.StartAt),
                TimeOnly.FromDateTime(t.EndAt),
                t.ShiftRefId))
            .ToList();
        var partition = await _partitionService.PartitionAsync(rows, plan.Token, overrideBlock, ct);

        var unplaceable = FindPlacementsWithoutClone(items, partition.AcceptedIndexes, shiftIdMap, plan.SourceAnalyseToken, jobId);
        var outsidePeriod = await FindOutsidePeriodIndexesAsync(
            rows, partition.AcceptedIndexes.Except(unplaceable).ToList(), plan.GroupId, jobId, ct);
        var acceptedTokens = partition.AcceptedIndexes.Except(unplaceable).Except(outsidePeriod).Select(i => items[i]).ToList();
        var skipped = BuildSkippedPlacements(rows, partition.BlockedIndexes, unplaceable, outsidePeriod);

        return (partition, acceptedTokens, skipped);
    }

    private async Task<IReadOnlyList<Guid>> AddPlannerWorksAsync(List<BulkWorkItem> bulkItems, WizardScenarioApplyPlan plan)
    {
        if (bulkItems.Count == 0)
        {
            return [];
        }

        var response = await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
        {
            Works = bulkItems,
            PeriodStart = plan.PeriodFrom,
            PeriodEnd = plan.PeriodUntil,
        }, PlanningPeriodCheck.SkipForPlannerOutput));
        return response.CreatedIds;
    }

    // Expenses of a displaced incumbent are payroll data and must never be dropped: they follow the
    // planner's work on the same slot; a slot left empty (blocked placement) is emptied, except for an
    // incumbent carrying expenses that have no other work of its employee to go to.
    private async Task ResolveDisplacedWorksAsync(
        WizardScenarioApplyPlan plan,
        IReadOnlyList<Guid> displacedWorkIds,
        IReadOnlyList<Guid> createdIds,
        Guid jobId,
        CancellationToken ct)
    {
        var expensesChangingEmployee = await _scenarioService.ResolveDisplacedWorksAsync(plan.Token, displacedWorkIds, createdIds, ct);
        await _unitOfWork.CompleteAsync();
        if (expensesChangingEmployee > 0)
        {
            _logger.LogInformation(
                "Wizard job {JobId}: {Count} expenses of replaced works moved to a work of another employee in scenario {Token}",
                jobId, expensesChangingEmployee, plan.Token);
        }
    }

    private static List<SkippedPlacementDto> BuildSkippedPlacements(
        IReadOnlyList<PlannedWorkRow> rows,
        IReadOnlyList<BlockedRow> blockedRows,
        IReadOnlyList<int>? withoutCloneIndexes = null,
        IReadOnlyList<int>? outsidePeriodIndexes = null)
        => blockedRows
            .Select(b => (b.Index, b.ReasonKey))
            .Concat((withoutCloneIndexes ?? []).Select(index => (Index: index, ReasonKey: ShiftNotInScenarioReasonKey)))
            .Concat((outsidePeriodIndexes ?? []).Select(index => (Index: index, ReasonKey: OutsidePlanningPeriodReasonKey)))
            .Select(s => new SkippedPlacementDto(
                rows[s.Index].ClientId,
                rows[s.Index].Date,
                rows[s.Index].ShiftId,
                s.ReasonKey))
            .ToList();

    /// <summary>
    /// The source scenario of a run started from a scenario. A source the caller may not see is refused like a missing
    /// one (404) rather than ignored: the cached result was already built from that scenario's data.
    /// </summary>
    private async Task<AnalyseScenario?> LoadSourceScenarioAsync(Guid? sourceAnalyseToken, CancellationToken ct)
    {
        if (sourceAnalyseToken is not Guid token)
        {
            return null;
        }

        await _scenarioTokenAccessGuard.EnsureVisibleAsync(token, ct);
        return await _scenarioRepository.GetByTokenAsync(token, ct);
    }

    /// <summary>
    /// Best-effort run-protocol capture for the (deferred) preference-learner. A capture failure must never
    /// break the apply — the planner's plan is more important than the measurement — so all faults are
    /// swallowed with a warning. The created work ids are the proposal set the measurement job later joins on.
    /// <para>
    /// ChurnAtApply is intentionally left null for BOTH Wizard-1 apply paths (direct and scenario). Wizard 1 is
    /// the first-fill planner: it creates the initial assignment rather than reworking an incumbent, so an
    /// apply-time edit-distance against the (empty) baseline is ~1.0 and measures nothing. Writing that pseudo
    /// value would only pollute the learner signal, hence null. The meaningful learner label is the deferred,
    /// event-cleaned CorrectionChurn, not ChurnAtApply.
    /// </para>
    /// </summary>
    private async Task CaptureRunAsync(
        Guid jobId,
        WizardApplyKind applyKind,
        Guid? scenarioId,
        Guid? runGroupId,
        Guid? groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        string subScoreJson,
        int stage0Violations,
        IReadOnlyList<Guid> createdWorkIds,
        CancellationToken ct)
    {
        if (createdWorkIds.Count == 0)
        {
            return;
        }

        try
        {
            var capture = new WizardRunCapture
            {
                Id = Guid.NewGuid(),
                Engine = WizardEngine.TokenEvolution,
                JobId = jobId,
                RunGroupId = runGroupId,
                ScenarioId = scenarioId,
                ApplyKind = applyKind,
                GroupId = groupId,
                PeriodFrom = periodFrom,
                PeriodUntil = periodUntil,
                SubScoreJson = subScoreJson,
                Stage0Violations = stage0Violations,
                ChurnAtApply = null,
            };

            await _captureRepository.AddAsync(capture, createdWorkIds, ct);

            if (applyKind == WizardApplyKind.Direct)
            {
                var superseded = await _captureRepository.SupersedeOpenDirectCapturesAsync(
                    capture.Id, WizardEngine.TokenEvolution, periodFrom, periodUntil, ct);

                if (superseded > 0)
                {
                    _logger.LogInformation(
                        "Direct apply for job {JobId} superseded {Count} still-open direct capture(s) overlapping {From}..{Until}.",
                        jobId, superseded, periodFrom, periodUntil);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WizardRunCapture write failed for job {JobId} ({ApplyKind}); apply itself is unaffected", jobId, applyKind);
        }
    }

    private async Task PersistEscalationsAsync(
        IReadOnlyList<BulkWorkItem> appliedWorks,
        IReadOnlyList<WizardEscalationDto> escalations,
        Guid? analyseToken,
        DateOnly periodStart,
        DateOnly periodEnd,
        CancellationToken ct)
    {
        var agentIds = appliedWorks.Select(w => w.ClientId).Distinct().ToList();
        var newRows = MapEscalations(escalations, analyseToken, agentIds, periodStart, periodEnd);
        await _softeningRepository.ReplaceForRangeAsync(agentIds, periodStart, periodEnd, analyseToken, newRows, ct);
        await _unitOfWork.CompleteAsync();
    }

    private static List<WorkSoftening> MapEscalations(
        IReadOnlyList<WizardEscalationDto> escalations,
        Guid? analyseToken,
        IReadOnlyList<Guid> appliedAgentIds,
        DateOnly periodStart,
        DateOnly periodEnd)
    {
        var rows = new List<WorkSoftening>(escalations.Count);
        var validAgents = new HashSet<Guid>(appliedAgentIds);
        foreach (var esc in escalations)
        {
            if (!Guid.TryParse(esc.AgentId, out var agentId) || !validAgents.Contains(agentId))
            {
                continue;
            }
            if (!DateOnly.TryParse(esc.Date, CultureInfo.InvariantCulture, out var date)
                || date < periodStart || date > periodEnd)
            {
                continue;
            }
            rows.Add(new WorkSoftening
            {
                ClientId = agentId,
                CurrentDate = date,
                Kind = MapKind(esc.RuleName),
                RuleName = esc.RuleName,
                Hint = esc.Hint,
                AnalyseToken = analyseToken,
            });
        }
        return rows;
    }

    private static SofteningKind MapKind(string ruleName)
    {
        return ruleName switch
        {
            "MinRestDays" => SofteningKind.MinRestDays,
            "MaxWorkDays" or "MaxConsecutiveDays" => SofteningKind.MaxConsecutiveWorkDays,
            "PreferredShift" or "Preferred" => SofteningKind.PreferredShiftViolation,
            "Blacklist" or "BlacklistedShift" => SofteningKind.BlacklistedShiftAssigned,
            "WeeklyHours" or "HeavyWeek" => SofteningKind.HeavyWeeklyLoad,
            _ => SofteningKind.Unknown,
        };
    }

    /// <summary>
    /// Accepted placements a scenario apply cannot write. From the real plan none: an unmapped shift falls back to the
    /// token's own (real) shift as before. From a source scenario every placement whose shift has no clone in the new
    /// scenario, because that shift belongs to the source scenario and a work on it would be invisible in the new scenario
    /// and promoted onto a foreign scenario shift on accept; they are reported as skipped instead.
    /// </summary>
    private List<int> FindPlacementsWithoutClone(
        IReadOnlyList<CoreToken> items,
        IReadOnlyList<int> acceptedIndexes,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        Guid? sourceAnalyseToken,
        Guid jobId)
    {
        if (sourceAnalyseToken is null)
        {
            return [];
        }

        var withoutClone = acceptedIndexes.Where(i => !shiftIdMap.ContainsKey(items[i].ShiftRefId)).ToList();
        if (withoutClone.Count > 0)
        {
            _logger.LogWarning(
                "WizardApply job {JobId}: {Count} placements on shifts without a clone in the new scenario were skipped: {ShiftIds}",
                jobId, withoutClone.Count, string.Join(",", withoutClone.Select(i => items[i].ShiftRefId).Distinct()));
        }

        return withoutClone;
    }

    /// <summary>
    /// Accepted placements on a day outside their employee's membership or group period. With a scenario group the group
    /// period of that group applies (the AutoWizard background run has no request, so this is what makes it behave like
    /// a planner who selected that group); without one, the request's group context decides, like every other write.
    /// </summary>
    private async Task<List<int>> FindOutsidePeriodIndexesAsync(
        IReadOnlyList<PlannedWorkRow> rows,
        IReadOnlyList<int> acceptedIndexes,
        Guid? groupId,
        Guid jobId,
        CancellationToken ct)
    {
        var cells = acceptedIndexes.Select(i => (rows[i].ClientId, rows[i].Date)).Distinct().ToList();
        var violations = groupId is Guid targetGroupId
            ? await _planningPeriodGuard.FindViolationsInGroupsAsync(cells, [targetGroupId], ct)
            : await _planningPeriodGuard.FindViolationsAsync(cells, ct);
        if (violations.Count == 0)
        {
            return [];
        }

        var outside = violations.Select(v => (v.ClientId, v.Date)).ToHashSet();
        var indexes = acceptedIndexes.Where(i => outside.Contains((rows[i].ClientId, rows[i].Date))).ToList();
        _logger.LogWarning(
            "WizardApply job {JobId}: {Count} placements outside the employees' planning period were skipped",
            jobId, indexes.Count);
        return indexes;
    }

    private static BulkWorkItem BuildBulkItem(CoreToken token, Guid? analyseToken, IReadOnlyDictionary<Guid, Guid>? shiftIdMap = null)
    {
        var shiftId = shiftIdMap != null && shiftIdMap.TryGetValue(token.ShiftRefId, out var mapped)
            ? mapped
            : token.ShiftRefId;

        return new BulkWorkItem
        {
            ClientId = Guid.Parse(token.AgentId),
            ShiftId = shiftId,
            CurrentDate = token.Date,
            StartTime = TimeOnly.FromDateTime(token.StartAt),
            EndTime = TimeOnly.FromDateTime(token.EndAt),
            WorkTime = token.TotalHours,
            Surcharges = token.Surcharges,
            Information = null,
            AnalyseToken = analyseToken,
        };
    }
}
