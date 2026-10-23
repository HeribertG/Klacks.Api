// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Wizard4;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Materialises a cached harmonizer result into a new AnalyseScenario. The source schedule
/// stays untouched so the user can compare or roll back. The cloned works are re-pointed in place
/// to the harmonised agent assignment, so their WorkChange/Expense/sub-Break children survive; only
/// bitmap works the clone cannot correlate, even through Work.ClonedFromWorkId, are recreated.
/// </summary>
/// <param name="resultCache">Cache populated by the harmonizer runner</param>
/// <param name="mediator">Mediator used to dispatch BulkAddWorksCommand</param>
/// <param name="scenarioRepository">Repository for persisting new AnalyseScenario entities</param>
/// <param name="scenarioService">Service for cloning schedule data into the new scenario</param>
/// <param name="unitOfWork">Unit of work for flushing EF changes</param>
/// <param name="context">EF Core database context for loading original Work metadata</param>
/// <param name="scenarioComplianceService">End-state compliance diff of the new scenario versus the real plan</param>
/// <param name="timelineService">Queues the scenario error-list refresh on the repoint branch (bulk-add refreshes itself)</param>
/// <param name="companyClock">Resolves the company's current calendar date when the best bitmap has no days</param>
/// <param name="scenarioNameGenerator">Builds the localized, per-group unique scenario name</param>
public class HarmonizerApplyService : IHarmonizerApplyService
{
    /// <summary>
    /// Kind of the localized scenario name prefix. Subclasses (e.g. <c>HolisticHarmonizerApplyService</c>)
    /// override this to distinguish their output in the scenario list.
    /// </summary>
    protected virtual ScenarioNameKind DefaultScenarioNameKind => ScenarioNameKind.Harmonized;

    /// <summary>
    /// Engine tag written onto the capture row. The base class is Wizard 2 (Harmonizer);
    /// <c>HolisticHarmonizerApplyService</c> overrides this to Wizard 3 (Holistic).
    /// </summary>
    protected virtual WizardEngine CaptureEngine => WizardEngine.Harmonizer;


    private readonly HarmonizerResultCache _resultCache;
    private readonly IMediator _mediator;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IAnalyseScenarioService _scenarioService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DataBaseContext _context;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IScenarioComplianceService _scenarioComplianceService;
    private readonly IScheduleTimelineService _timelineService;
    private readonly IScheduleSnapshotMarkerService _snapshotMarkerService;
    private readonly ICompanyClock _companyClock;
    private readonly IScenarioNameGenerator _scenarioNameGenerator;
    private readonly IScenarioTokenAccessGuard _scenarioTokenAccessGuard;
    private readonly ILogger _logger;

    public HarmonizerApplyService(
        HarmonizerResultCache resultCache,
        IMediator mediator,
        IAnalyseScenarioRepository scenarioRepository,
        IAnalyseScenarioService scenarioService,
        IUnitOfWork unitOfWork,
        DataBaseContext context,
        IWizardRunCaptureRepository captureRepository,
        IScenarioComplianceService scenarioComplianceService,
        IScheduleTimelineService timelineService,
        IScheduleSnapshotMarkerService snapshotMarkerService,
        ICompanyClock companyClock,
        IScenarioNameGenerator scenarioNameGenerator,
        IScenarioTokenAccessGuard scenarioTokenAccessGuard,
        ILogger<HarmonizerApplyService> logger)
    {
        _scenarioTokenAccessGuard = scenarioTokenAccessGuard;
        _resultCache = resultCache;
        _mediator = mediator;
        _scenarioRepository = scenarioRepository;
        _scenarioService = scenarioService;
        _unitOfWork = unitOfWork;
        _context = context;
        _captureRepository = captureRepository;
        _scenarioComplianceService = scenarioComplianceService;
        _timelineService = timelineService;
        _snapshotMarkerService = snapshotMarkerService;
        _companyClock = companyClock;
        _scenarioNameGenerator = scenarioNameGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Compares the schedule against the fingerprint taken when the run started. A run without a marker
    /// (older cache entry, or a caller that does not record one) is applied unchanged.
    /// </summary>
    private async Task EnsureSnapshotUnchangedAsync(
        Guid jobId,
        ScheduleSnapshotMarker? marker,
        HarmonyBitmap? originalBitmap,
        HarmonyBitmap bestBitmap,
        Guid? sourceAnalyseToken,
        string subScoreJson,
        int stage0Violations,
        CancellationToken ct)
    {
        if (marker is null)
        {
            return;
        }

        var current = await _snapshotMarkerService.ComputeAsync(
            marker.From, marker.Until, marker.AgentIds, marker.AnalyseToken, ct);

        var placementChanged = current.PlacementHash != marker.PlacementHash;
        if (current.MovableWorkCount == marker.MovableWorkCount
            && current.StandaloneBreakCount == marker.StandaloneBreakCount
            && !placementChanged)
        {
            return;
        }

        // Put the result back: the operator can re-run, or deliberately repeat the apply.
        _resultCache.Store(jobId, originalBitmap!, bestBitmap, sourceAnalyseToken, subScoreJson, stage0Violations, marker);

        var detail = placementChanged
            && current.MovableWorkCount == marker.MovableWorkCount
            && current.StandaloneBreakCount == marker.StandaloneBreakCount
            ? "placement changed"
            : $"works {marker.MovableWorkCount} -> {current.MovableWorkCount}, standalone breaks {marker.StandaloneBreakCount} -> {current.StandaloneBreakCount}";

        _logger.LogInformation(
            "Harmonizer job {JobId} apply rejected as stale: {Detail}", jobId, detail);

        throw new StaleWizardResultException(
            $"The schedule changed since this run started ({detail}). Re-run the wizard or repeat the apply deliberately.",
            marker.MovableWorkCount,
            current.MovableWorkCount,
            marker.StandaloneBreakCount,
            current.StandaloneBreakCount,
            placementChanged);
    }

    public async Task<(AnalyseScenarioResource Scenario, IReadOnlyList<Guid> CreatedWorkIds, ScenarioComplianceReport? ComplianceReport)> ApplyAsScenarioAsync(
        Guid jobId,
        Guid? groupId,
        CancellationToken ct,
        ScenarioNameKind? nameKind = null,
        string? language = null,
        bool captureRun = true,
        bool evaluateCompliance = true)
    {
        // Taking the entry out atomically is what stops a double apply from creating two scenarios;
        // every path that does not consume it puts it back.
        if (!_resultCache.TryTake(jobId, out var originalBitmap, out var bestBitmap, out var sourceAnalyseToken, out var subScoreJson, out var stage0Violations, out var snapshotMarker) || bestBitmap is null)
        {
            throw new InvalidOperationException($"No cached harmonizer result for job id {jobId}.");
        }

        try
        {
            await EnsureSnapshotUnchangedAsync(jobId, snapshotMarker, originalBitmap, bestBitmap, sourceAnalyseToken, subScoreJson, stage0Violations, ct);

            var plan = await BuildScenarioPlanAsync(jobId, bestBitmap, sourceAnalyseToken, groupId, nameKind ?? DefaultScenarioNameKind, language, ct);

            // Scenario row, clone and materialisation must land together - a failure in between used to
            // leave a scenario whose schedule was half-written.
            var materialised = await _unitOfWork.ExecuteInTransactionAsync(() => MaterialiseScenarioAsync(plan, ct));

            // Range checks run on a background context that would read the pre-commit state from inside
            // the transaction; re-queueing after the commit is idempotent and heals that.
            _timelineService.QueueRangeCheck(plan.ScenarioFrom, plan.ScenarioUntil, materialised.Token);

            if (captureRun)
            {
                await CaptureRunAsync(
                    jobId,
                    materialised.Scenario.Id,
                    plan.RunGroupId,
                    plan.GroupId,
                    plan.PeriodFrom,
                    plan.PeriodUntil,
                    subScoreJson,
                    stage0Violations,
                    BitmapChurn.Ratio(originalBitmap!, bestBitmap),
                    materialised.Ids,
                    ct);
            }

            // End-state verdict AFTER all materialisation is flushed: the loader reads the DB, so it
            // sees exactly the scenario the user will inspect. A per-row partition would double-count
            // redistributions, hence the diff-based report.
            ScenarioComplianceReport? complianceReport = null;
            if (evaluateCompliance)
            {
                complianceReport = await _scenarioComplianceService.EvaluateAsync(
                    plan.PeriodFrom, plan.PeriodUntil, plan.GroupId, materialised.Token, ct);
            }

            return (AppliedScenarioBuilder.ToResource(materialised.Scenario), materialised.Ids, complianceReport);
        }
        catch
        {
            _resultCache.Store(jobId, originalBitmap!, bestBitmap, sourceAnalyseToken, subScoreJson, stage0Violations, snapshotMarker);
            throw;
        }
    }

    private async Task<HarmonizerScenarioApplyPlan> BuildScenarioPlanAsync(
        Guid jobId,
        HarmonyBitmap bestBitmap,
        Guid? sourceAnalyseToken,
        Guid? requestedGroupId,
        ScenarioNameKind nameKind,
        string? language,
        CancellationToken ct)
    {
        var sourceScenario = await LoadSourceScenarioAsync(sourceAnalyseToken, ct);
        var runGroupId = sourceScenario?.RunGroupId ?? Guid.NewGuid();
        var groupId = ScenarioGroupInheritance.Resolve(requestedGroupId, sourceScenario);

        var workIds = CollectWorkIds(bestBitmap);
        var originalWorks = await LoadWorksAsync(workIds, ct);

        var periodFrom = bestBitmap.Days.Count > 0 ? bestBitmap.Days[0] : await _companyClock.GetTodayDateAsync(ct);
        var periodUntil = bestBitmap.Days.Count > 0 ? bestBitmap.Days[^1] : periodFrom;
        var (scenarioFrom, scenarioUntil) = ScenarioSourceRange.Widen(
            periodFrom, periodUntil, sourceScenario?.FromDate, sourceScenario?.UntilDate);

        var bitmapShiftIds = CollectBitmapShiftIds(bestBitmap, originalWorks);
        var shiftIdsToClone = await ResolveShiftIdsToCloneAsync(groupId, sourceAnalyseToken, bitmapShiftIds, ct);

        _logger.LogInformation(
            "HarmonizerApply jobId={JobId} bitmap rows={Rows} days={Days} periodFrom={From} periodUntil={Until} workIds={WorkIds} originalWorks={OriginalWorks} bitmapShifts={BitmapShifts}",
            jobId, bestBitmap.RowCount, bestBitmap.DayCount, periodFrom, periodUntil, workIds.Count, originalWorks.Count, bitmapShiftIds.Count);

        return new HarmonizerScenarioApplyPlan(
            jobId, bestBitmap, sourceAnalyseToken, groupId, runGroupId, nameKind, language, workIds, originalWorks,
            periodFrom, periodUntil, scenarioFrom, scenarioUntil, shiftIdsToClone);
    }

    private async Task<(IReadOnlyList<Guid> Ids, Guid Token, AnalyseScenario Scenario)> MaterialiseScenarioAsync(HarmonizerScenarioApplyPlan plan, CancellationToken ct)
    {
        // A transient retry replays this delegate; without clearing, the first attempt's entities
        // would still be tracked and inserted a second time.
        _context.ChangeTracker.Clear();

        var (analyseScenario, shiftIdMap, workIdMap) = await CreateScenarioWithCloneAsync(plan, ct);
        var token = analyseScenario.Token;

        var correlation = await CorrelateClonedWorksAsync(plan, workIdMap, ct);
        var correlatedWorkIdMap = correlation.WorkIdMap;

        var ids = new List<Guid>(await RepointClonedWorksAsync(
            token, plan.PeriodFrom, plan.PeriodUntil, plan.BestBitmap, correlatedWorkIdMap, ct, correlation.SupersededCloneIds));
        await _unitOfWork.CompleteAsync();
        _logger.LogInformation(
            "HarmonizerApply jobId={JobId} re-pointed {Count} cloned works in place (children preserved).",
            plan.JobId, ids.Count);

        if (plan.SourceAnalyseToken is not null)
        {
            ids.AddRange(await RecreateUncorrelatedWorksAsync(
                plan.JobId, plan.BestBitmap, plan.OriginalWorks, correlatedWorkIdMap, correlation.SupersededBitmapWorkIds, shiftIdMap, token, plan.SourceAnalyseToken, plan.PeriodFrom, plan.PeriodUntil, ct));
        }

        return (ids, token, analyseScenario);
    }

    private async Task<(AnalyseScenario Scenario, Dictionary<Guid, Guid> ShiftIdMap, Dictionary<Guid, Guid> WorkIdMap)> CreateScenarioWithCloneAsync(
        HarmonizerScenarioApplyPlan plan,
        CancellationToken ct)
    {
        var name = await _scenarioNameGenerator.GenerateAsync(
            plan.NameKind, plan.PeriodFrom, plan.PeriodUntil, plan.GroupId, plan.Language, ct);
        var token = Guid.NewGuid();

        var analyseScenario = AppliedScenarioBuilder.Create(
            name, plan.GroupId, plan.ScenarioFrom, plan.ScenarioUntil, token, plan.RunGroupId, plan.SourceAnalyseToken);

        await _scenarioRepository.Add(analyseScenario);
        var (shiftIdMap, workIdMap) = await _scenarioService.CloneScenarioDataWithMapsAsync(
            plan.GroupId, plan.ScenarioFrom, plan.ScenarioUntil, token, plan.ShiftIdsToClone, plan.SourceAnalyseToken, ct);
        await _unitOfWork.CompleteAsync();

        return (analyseScenario, shiftIdMap, workIdMap);
    }

    // CloneScenarioDataAsync clones the source schedule (Works + their WorkChange/Expense/sub-Break
    // children) into the new scenario; workIdMap maps every cloned source work id to its clone. The
    // harmonised result is materialised by RE-POINTING the movable clones in place (ClientId/date from
    // the bitmap), so expenses, corrections and pauses survive (C4; payroll export must never lose an
    // expense). This holds for a scenario source too: the bitmap holds the source scenario's work ids,
    // and inside the source scenario's range the clone copies exactly those works.
    // Outside that range the clone takes the real plan, while the bitmap carries the source scenario's
    // boundary copies; those are correlated through Work.ClonedFromWorkId (copy -> real work -> clone) when
    // shift and date still match, else the real plan's state wins (ScenarioSourceWorkCorrelator); legacy
    // copies without the link fall back to a unique key match. Only works correlated neither way are
    // recreated from the bitmap without their children, with a warning; their real-plan clones have no
    // re-point target and are removed by the re-point. Locked works, replacement covers and absence
    // breaks are left intact either way.
    private async Task<ScenarioSourceWorkCorrelation> CorrelateClonedWorksAsync(
        HarmonizerScenarioApplyPlan plan,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        CancellationToken ct)
    {
        var correlation = plan.SourceAnalyseToken is null
            ? ScenarioSourceWorkCorrelation.Direct(workIdMap)
            : await new ScenarioSourceWorkCorrelator(_context, _scenarioService)
                .CorrelateAsync(plan.WorkIds, plan.OriginalWorks, workIdMap, ct);
        if (correlation.SupersededBitmapWorkIds.Count > 0)
        {
            _logger.LogWarning(
                "HarmonizerApply jobId={JobId} (scenario source): {Count} bitmap works stand for real works that moved to another shift or date since the source scenario was made; the real plan's state is kept for them (not re-pointed, not recreated).",
                plan.JobId, correlation.SupersededBitmapWorkIds.Count);
        }

        return correlation;
    }

    /// <summary>
    /// Best-effort run-protocol capture for the (deferred) preference-learner. A capture failure must never
    /// break the apply — the harmonised plan matters more than the measurement — so all faults are swallowed
    /// with a warning. The created work ids are the proposal set the measurement job later joins on.
    /// </summary>
    private async Task CaptureRunAsync(
        Guid jobId,
        Guid scenarioId,
        Guid? runGroupId,
        Guid? groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        string subScoreJson,
        int stage0Violations,
        double churnAtApply,
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
                Engine = CaptureEngine,
                JobId = jobId,
                RunGroupId = runGroupId,
                ScenarioId = scenarioId,
                ApplyKind = WizardApplyKind.Scenario,
                GroupId = groupId,
                PeriodFrom = periodFrom,
                PeriodUntil = periodUntil,
                SubScoreJson = subScoreJson,
                Stage0Violations = stage0Violations,
                ChurnAtApply = churnAtApply,
            };

            await _captureRepository.AddAsync(capture, createdWorkIds, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WizardRunCapture write failed for harmonizer job {JobId} ({Engine}); apply itself is unaffected", jobId, CaptureEngine);
        }
    }

    private static List<Guid> CollectWorkIds(HarmonyBitmap bitmap)
    {
        var ids = new HashSet<Guid>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                foreach (var id in cell.WorkIds)
                {
                    ids.Add(id);
                }
            }
        }
        return ids.ToList();
    }

    /// <summary>
    /// Collects every original Shift ID referenced by the bitmap so the scenario
    /// clone covers them even when they have no GroupItem in the resolved group
    /// hierarchy. The harmonizer can carry shifts (e.g. container/sporadic) that
    /// the GroupItem-based clone filter misses; without these IDs the resulting
    /// shiftIdMap would be incomplete and BuildBulkItems would skip those works,
    /// leaving the scenario schedule empty after Apply.
    /// </summary>
    private static HashSet<Guid> CollectBitmapShiftIds(HarmonyBitmap bitmap, IReadOnlyDictionary<Guid, Work> originalWorks)
    {
        var ids = new HashSet<Guid>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                if (cell.ShiftRefId is { } shiftRefId)
                {
                    ids.Add(shiftRefId);
                }
            }
        }
        foreach (var work in originalWorks.Values)
        {
            ids.Add(work.ShiftId);
        }
        return ids;
    }

    /// <summary>
    /// Shifts the new scenario must carry. A group-scoped clone adds the group's own shifts to the listed ones; without a
    /// group the listed ids are the exact set, so a scenario source contributes all of its shifts (a shift without a work
    /// is not in the bitmap) and a real source lists nothing, which clones every real shift.
    /// </summary>
    private async Task<IReadOnlyCollection<Guid>> ResolveShiftIdsToCloneAsync(
        Guid? groupId,
        Guid? sourceAnalyseToken,
        IReadOnlyCollection<Guid> bitmapShiftIds,
        CancellationToken ct)
    {
        if (groupId.HasValue)
        {
            return bitmapShiftIds;
        }

        if (sourceAnalyseToken is not Guid sourceToken)
        {
            return [];
        }

        var sourceShiftIds = await _context.Shift.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(s => s.AnalyseToken == sourceToken && !s.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync(ct);
        return sourceShiftIds.Concat(bitmapShiftIds).Distinct().ToList();
    }

    private async Task<Dictionary<Guid, Work>> LoadWorksAsync(IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
        {
            return [];
        }
        var works = await _context.Work
            .AsNoTracking()
            .Where(w => ids.Contains(w.Id))
            .ToListAsync(ct);
        return works.ToDictionary(w => w.Id);
    }

    /// <summary>
    /// Recreates from the bitmap the works the clone cannot correlate, neither through ClonedFromWorkId nor by a unique
    /// key match (a legacy source scenario with ambiguous keys, or a real work deleted since). Their real-plan clones were
    /// removed by the re-point (no target), so without this step the harmonised works of those days would be lost. Locked
    /// works and replacement covers are skipped by BuildBulkItems as before; superseded works (real origin moved) are
    /// never recreated, the real plan's state wins.
    /// </summary>
    /// <param name="supersededBitmapWorkIds">Bitmap works whose real origin moved since the source scenario was made</param>
    /// <param name="sourceAnalyseToken">Token of the source scenario whose replacement covers stay fixed</param>
    /// <returns>Ids of the recreated works</returns>
    private async Task<IReadOnlyList<Guid>> RecreateUncorrelatedWorksAsync(
        Guid jobId,
        HarmonyBitmap bitmap,
        IReadOnlyDictionary<Guid, Work> originalWorks,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        IReadOnlySet<Guid> supersededBitmapWorkIds,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        Guid token,
        Guid? sourceAnalyseToken,
        DateOnly periodFrom,
        DateOnly periodUntil,
        CancellationToken ct)
    {
        var uncorrelatedWorkIds = CollectWorkIds(bitmap)
            .Where(id => !workIdMap.ContainsKey(id) && !supersededBitmapWorkIds.Contains(id))
            .ToHashSet();
        if (uncorrelatedWorkIds.Count == 0)
        {
            return [];
        }

        var sourceFixedWorkIds = await CorrectedWorkQuery.LoadFixedWorkIdsAsync(
            _context, sourceAnalyseToken, periodFrom, periodUntil, ct);
        var bulkItems = BuildBulkItems(bitmap, originalWorks, shiftIdMap, token, sourceFixedWorkIds, uncorrelatedWorkIds);
        _logger.LogWarning(
            "HarmonizerApply jobId={JobId} (scenario source): {Uncorrelated} bitmap works lie outside the source scenario's range and could be correlated neither through ClonedFromWorkId nor by a unique key; recreating {Items} of them without their WorkChange/Expense children.",
            jobId, uncorrelatedWorkIds.Count, bulkItems.Count);

        if (bulkItems.Count == 0)
        {
            return [];
        }

        var response = await _mediator.Send(new BulkAddWorksCommand(new BulkAddWorksRequest
        {
            Works = bulkItems,
            PeriodStart = periodFrom,
            PeriodEnd = periodUntil,
        }, PlanningPeriodCheck.SkipForPlannerOutput));
        return response.CreatedIds;
    }

    /// <summary>
    /// Re-points the movable cloned works in place to the agents the harmonised bitmap assigns them to,
    /// instead of delete+recreate. The clone already carries the original work's shift, times and
    /// WorkChange/Expense/sub-Break children (CloneWorks), so changing only the ClientId preserves those
    /// children on accept (C4). The harmonizer reassigns agents within fixed (shift, date) columns, so
    /// the date stays invariant and the children remain consistent. Movable clones the bitmap no longer
    /// assigns are soft-deleted with their children; locked works and absence breaks are never touched.
    /// Returns the ids of the re-pointed works.
    /// </summary>
    internal async Task<IReadOnlyList<Guid>> RepointClonedWorksAsync(
        Guid token,
        DateOnly from,
        DateOnly until,
        HarmonyBitmap bitmap,
        IReadOnlyDictionary<Guid, Guid> workIdMap,
        CancellationToken ct,
        IReadOnlySet<Guid>? keptCloneIds = null)
    {
        var targets = new Dictionary<Guid, (Guid Agent, DateOnly Date)>();
        var keptInLockedCells = new HashSet<Guid>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            if (!Guid.TryParse(bitmap.Rows[r].Id, out var agentId))
            {
                continue;
            }
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                if (cell.IsLocked)
                {
                    // A locked cell never moves, so every work merged into it stays where it is - including an
                    // unlocked work that only shares the day with a locked work, a break or a replacement cover.
                    foreach (var realWorkId in cell.WorkIds)
                    {
                        if (workIdMap.TryGetValue(realWorkId, out var keptId))
                        {
                            keptInLockedCells.Add(keptId);
                        }
                    }
                    continue;
                }
                if (cell.Symbol == CellSymbol.Free || cell.Symbol == CellSymbol.Break || cell.WorkIds.Count == 0)
                {
                    continue;
                }
                foreach (var realWorkId in cell.WorkIds)
                {
                    if (workIdMap.TryGetValue(realWorkId, out var cloneId))
                    {
                        targets[cloneId] = (agentId, bitmap.Days[d]);
                    }
                }
            }
        }

        // A work handed to a substitute (replacement WorkChange) or carrying a correction is locked in the bitmap and
        // therefore never a target; without this exclusion it would be soft-deleted together with its children.
        var fixedWorkIds = await CorrectedWorkQuery.LoadFixedWorkIdsAsync(_context, token, from, until, ct);
        var movableWorks = (await _context.Work.IgnoreQueryFilters()
            .Where(w => w.AnalyseToken == token && w.CurrentDate >= from && w.CurrentDate <= until
                && !w.IsDeleted && w.LockLevel == WorkLockLevel.None)
            .ToListAsync(ct))
            .Where(w => !fixedWorkIds.Contains(w.Id) && !(w.ParentWorkId.HasValue && fixedWorkIds.Contains(w.ParentWorkId.Value)))
            .Where(w => !keptInLockedCells.Contains(w.Id) && !(w.ParentWorkId.HasValue && keptInLockedCells.Contains(w.ParentWorkId.Value)))
            .Where(w => keptCloneIds is null
                || (!keptCloneIds.Contains(w.Id) && !(w.ParentWorkId.HasValue && keptCloneIds.Contains(w.ParentWorkId.Value))))
            .ToList();

        // Apply each work's new (agent, date) and remember it so the work's children can follow. Setting
        // both ClientId and CurrentDate keeps the result identical to the delete+recreate BuildBulkItems.
        var appliedTargets = new Dictionary<Guid, (Guid Agent, DateOnly Date)>();
        var toDelete = new List<Work>();
        foreach (var w in movableWorks)
        {
            if (targets.TryGetValue(w.Id, out var target)
                || (w.ParentWorkId.HasValue && targets.TryGetValue(w.ParentWorkId.Value, out target)))
            {
                w.ClientId = target.Agent;
                w.CurrentDate = target.Date;
                appliedTargets[w.Id] = target;
            }
            else
            {
                toDelete.Add(w);
            }
        }

        // Sub-breaks (work pauses) carry their OWN ClientId/CurrentDate (ScheduleEntryBase), unlike
        // WorkChange/Expenses which the schedule SP derives from the parent work. Re-point them with their
        // parent so period-hours and any client-keyed aggregation stay attributed to the new agent.
        if (appliedTargets.Count > 0)
        {
            var repointedIds = appliedTargets.Keys.ToList();
            var subBreaks = await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && repointedIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
            foreach (var b in subBreaks)
            {
                var target = appliedTargets[b.ParentWorkId!.Value];
                b.ClientId = target.Agent;
                b.CurrentDate = target.Date;
            }
        }

        if (toDelete.Count > 0)
        {
            var now = DateTime.UtcNow;
            var deleteIds = toDelete.Select(w => w.Id).ToList();

            var subBreaks = await _context.Break.IgnoreQueryFilters()
                .Where(b => !b.IsDeleted && b.ParentWorkId.HasValue && deleteIds.Contains(b.ParentWorkId.Value))
                .ToListAsync(ct);
            foreach (var b in subBreaks) { b.IsDeleted = true; b.DeletedTime = now; }

            var workChanges = await _context.WorkChange
                .Where(wc => !wc.IsDeleted && deleteIds.Contains(wc.WorkId))
                .ToListAsync(ct);
            foreach (var wc in workChanges) { wc.IsDeleted = true; wc.DeletedTime = now; }

            var expenses = await _context.Expenses
                .Where(e => !e.IsDeleted && deleteIds.Contains(e.WorkId))
                .ToListAsync(ct);
            foreach (var e in expenses) { e.IsDeleted = true; e.DeletedTime = now; }

            foreach (var w in toDelete) { w.IsDeleted = true; w.DeletedTime = now; }
        }

        return appliedTargets.Keys.ToList();
    }

    private List<BulkWorkItem> BuildBulkItems(
        HarmonyBitmap bitmap,
        IReadOnlyDictionary<Guid, Work> originalWorks,
        IReadOnlyDictionary<Guid, Guid> shiftIdMap,
        Guid analyseToken,
        IReadOnlySet<Guid> sourceFixedWorkIds,
        IReadOnlySet<Guid> onlyWorkIds)
    {
        var items = new List<BulkWorkItem>();
        var unmappedShifts = new HashSet<Guid>();
        for (var r = 0; r < bitmap.RowCount; r++)
        {
            var newAgentId = Guid.Parse(bitmap.Rows[r].Id);
            for (var d = 0; d < bitmap.DayCount; d++)
            {
                var cell = bitmap.GetCell(r, d);
                if (cell.Symbol == CellSymbol.Free || cell.WorkIds.Count == 0)
                {
                    continue;
                }

                // Locked works are kept by the clone and must not be rebuilt unlocked; the same holds for replacement
                // covers. An unlocked work that only shares a locked or absence cell did not move either, so it is
                // re-materialised where it is instead of being lost.
                var fixedCell = cell.IsLocked;
                foreach (var workId in cell.WorkIds)
                {
                    if (!onlyWorkIds.Contains(workId) || !originalWorks.TryGetValue(workId, out var original))
                    {
                        continue;
                    }

                    if (fixedCell && (original.LockLevel != WorkLockLevel.None
                        || sourceFixedWorkIds.Contains(original.Id)
                        || (original.ParentWorkId is { } parentId && sourceFixedWorkIds.Contains(parentId))))
                    {
                        continue;
                    }

                    // The cloned scenario only contains shifts mapped by CloneScenarioDataAsync.
                    // Falling back to the unmapped main-scenario shift_id sets the new Work's
                    // shift onto a Shift whose analyse_token is null instead of the new scenario
                    // token. get_schedule_entries.sql filters works through filtered_shift_ids
                    // (s.analyse_token IS NOT DISTINCT FROM p_analyse_token) and drops the work,
                    // making the schedule appear empty. Skip such items and surface them via the
                    // log so the missing-clone data issue is diagnosable.
                    if (!shiftIdMap.TryGetValue(original.ShiftId, out var mappedShiftId))
                    {
                        unmappedShifts.Add(original.ShiftId);
                        continue;
                    }

                    items.Add(new BulkWorkItem
                    {
                        ClientId = newAgentId,
                        ShiftId = mappedShiftId,
                        CurrentDate = bitmap.Days[d],
                        StartTime = original.StartTime,
                        EndTime = original.EndTime,
                        WorkTime = original.WorkTime,
                        Surcharges = original.Surcharges,
                        Information = original.Information,
                        AnalyseToken = analyseToken,
                    });
                }
            }
        }

        if (unmappedShifts.Count > 0)
        {
            _logger.LogWarning(
                "HarmonizerApply: {Count} original shifts had no entry in the cloned-scenario shiftIdMap and were skipped: {ShiftIds}. CloneScenarioDataAsync did not clone these shifts into the new scenario.",
                unmappedShifts.Count,
                string.Join(",", unmappedShifts));
        }

        return items;
    }

    private async Task<AnalyseScenario?> LoadSourceScenarioAsync(Guid? sourceAnalyseToken, CancellationToken ct)
    {
        if (sourceAnalyseToken is not Guid token)
        {
            return null;
        }
        await _scenarioTokenAccessGuard.EnsureVisibleAsync(token, ct);
        return await _scenarioRepository.GetByTokenAsync(token, ct);
    }
}
