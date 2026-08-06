// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.ScheduleOptimizer.Harmonizer.Bitmap;
using Klacks.ScheduleOptimizer.Harmonizer.Conductor;
using Klacks.ScheduleOptimizer.Harmonizer.Evolution;
using Klacks.ScheduleOptimizer.Wizard4;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Orchestrates one Wizard-4 optimisation pass and turns a meaningful improvement into a candidate
/// AnalyseScenario. Reuses the harmonizer infrastructure: the bitmap + objective contexts are built
/// from the real plan, the engine core runs keep-if-better against the composite objective, and the
/// best result is materialised through <see cref="IHarmonizerApplyService"/> (same clone/locked-work/
/// break-preserving path the W2/W3 apply uses). The composite sub-score vector + churn are captured
/// onto the candidate for the (deferred) preference-learning flywheel.
/// </summary>
public sealed class Wizard4Runner : IWizard4Runner
{
    private const string ScenarioPrefix = "Optimizer";
    private const string SystemActor = Wizard4LifecycleConstants.SystemActor;

    private readonly IHarmonizerContextBuilder _harmonizerContextBuilder;
    private readonly IWizardContextBuilder _wizardContextBuilder;
    private readonly IWizard4OptimizationCore _core;
    private readonly HarmonizerResultCache _resultCache;
    private readonly IHarmonizerApplyService _applyService;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IScheduleSnapshotMarkerService _snapshotMarkerService;
    private readonly IWizard4SnapshotGuard _snapshotGuard;
    private readonly IWizard4CandidateLifecycleService _lifecycleService;
    private readonly IWorkNotificationService _notificationService;
    private readonly ILogger<Wizard4Runner> _logger;

    public Wizard4Runner(
        IHarmonizerContextBuilder harmonizerContextBuilder,
        IWizardContextBuilder wizardContextBuilder,
        IWizard4OptimizationCore core,
        HarmonizerResultCache resultCache,
        IHarmonizerApplyService applyService,
        IAnalyseScenarioRepository scenarioRepository,
        IWizardRunCaptureRepository captureRepository,
        IUnitOfWork unitOfWork,
        IScheduleSnapshotMarkerService snapshotMarkerService,
        IWizard4SnapshotGuard snapshotGuard,
        IWizard4CandidateLifecycleService lifecycleService,
        IWorkNotificationService notificationService,
        ILogger<Wizard4Runner> logger)
    {
        _harmonizerContextBuilder = harmonizerContextBuilder;
        _wizardContextBuilder = wizardContextBuilder;
        _core = core;
        _resultCache = resultCache;
        _applyService = applyService;
        _scenarioRepository = scenarioRepository;
        _captureRepository = captureRepository;
        _unitOfWork = unitOfWork;
        _snapshotMarkerService = snapshotMarkerService;
        _snapshotGuard = snapshotGuard;
        _lifecycleService = lifecycleService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<AnalyseScenarioResource?> RunOnceAsync(
        Guid? groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        IReadOnlyList<Guid> agentIds,
        TimeSpan budget,
        CancellationToken ct)
    {
        // W4 optimises the real/accepted plan (AnalyseToken == null). The pass reads it twice - as a
        // bitmap and as an objective context - and a change between those reads would produce a snapshot
        // that never existed, so both run inside one repeatable-read transaction.
        var (bitmapInput, objectiveContext, fingerprintBefore) = await _snapshotGuard.ExecuteInSnapshotAsync(
            async () =>
            {
                var bitmap = await _harmonizerContextBuilder.BuildContextAsync(
                    new HarmonizerContextRequest(periodFrom, periodUntil, agentIds, AnalyseToken: null), ct);
                var objective = await _wizardContextBuilder.BuildContextAsync(
                    new WizardContextRequest(periodFrom, periodUntil, agentIds, ShiftIds: null, AnalyseToken: null), ct);
                var fingerprint = await _snapshotGuard.ComputeFingerprintAsync(agentIds, periodFrom, periodUntil, ct);
                return (bitmap, objective, fingerprint);
            },
            ct);

        var seed = RowSorter.Sort(BitmapBuilder.Build(bitmapInput));
        var validator = new DomainAwareReplaceValidator(
            bitmapInput.Availability, bitmapInput.BoundaryAssignments, bitmapInput.IneligibleAssignments);
        var config = new HarmonizerEvolutionConfig(MaxRuntime: budget);

        // Fingerprint of the plan this pass optimises; apply rejects the candidate if it moved meanwhile.
        var snapshotMarker = await _snapshotMarkerService.ComputeAsync(
            periodFrom, periodUntil, agentIds, analyseToken: null, ct);

        var result = _core.Optimize(seed, objectiveContext, validator, config, hints: bitmapInput.SofteningHints, ct: ct);

        // A pass takes minutes. If the plan moved meanwhile the candidate describes a schedule that no
        // longer exists, and materialising it would offer the planner a scenario built on stale ground.
        var fingerprintAfter = await _snapshotGuard.ComputeFingerprintAsync(agentIds, periodFrom, periodUntil, ct);
        if (fingerprintAfter != fingerprintBefore)
        {
            _logger.LogInformation(
                "Wizard4 discarded its result for group {GroupId}: the plan changed during the pass.", groupId);
            return null;
        }

        return await MaterializeCandidateIfImprovedAsync(
            result, seed, groupId, ct, snapshotMarker, periodFrom, periodUntil);
    }

    /// <summary>
    /// Public for unit testing: decides whether the run beat the snapshot by the improvement threshold
    /// and, if so, materialises the candidate scenario and captures the score snapshot.
    /// </summary>
    public async Task<AnalyseScenarioResource?> MaterializeCandidateIfImprovedAsync(
        Wizard4OptimizationResult result,
        HarmonyBitmap seed,
        Guid? groupId,
        CancellationToken ct,
        ScheduleSnapshotMarker? snapshotMarker,
        DateOnly periodFrom,
        DateOnly periodUntil)
    {
        if (result.BestFitness <= result.BaselineScalar + Wizard4Constants.MinImprovement)
        {
            _logger.LogInformation(
                "Wizard4 found no improvement beyond threshold (baseline {Baseline:F4}, best {Best:F4}); no candidate created.",
                result.BaselineScalar, result.BestFitness);
            return null;
        }

        // A newer candidate for the same selection replaces the older one instead of standing next to
        // it - nobody asked for either, and a stack of near-identical suggestions is worse than none.
        var previous = await _scenarioRepository.GetActiveCandidateAsync(
            SystemActor, groupId, periodFrom, periodUntil, ct);

        var jobId = Guid.NewGuid();
        _resultCache.Store(jobId, seed, result.BestBitmap, sourceAnalyseToken: null, snapshotMarker: snapshotMarker);
        // captureRun: false — the shared harmonizer apply must not write a Harmonizer-tagged capture on the
        // W4 path; this runner writes its own Wizard4 (composite) capture below, correlated by the same jobId.
        // evaluateCompliance: false — autonomous background path with no reader for the report; the real
        // plan is protected at the accept gate when a user promotes the candidate.
        var (resource, createdIds, _) = await _applyService.ApplyAsScenarioAsync(
            jobId, groupId, ct, ScenarioPrefix, captureRun: false, evaluateCompliance: false);

        var churn = BitmapChurn.Ratio(seed, result.BestBitmap);
        var scenario = await _scenarioRepository.Get(resource.Id);
        if (scenario is not null)
        {
            var subScoreJson = ScenarioScoreSerializer.Serialize(result.Best, result.BaselineScalar);
            var stage0Violations = result.Best.Gate.MandatoryQualMissing + result.Best.Gate.Legality;
            scenario.SubScoreJson = subScoreJson;
            scenario.ChurnRatio = churn;
            scenario.Stage0Violations = stage0Violations;
            scenario.CreatedByUser = SystemActor;
            await _scenarioRepository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            await CaptureRunAsync(jobId, resource, scenario.RunGroupId, subScoreJson, stage0Violations, churn, createdIds, ct);
        }

        if (previous is not null && previous.Id != resource.Id)
        {
            await _lifecycleService.SupersedeAsync(previous, ct);
        }

        _logger.LogInformation(
            "Wizard4 created candidate scenario {ScenarioId} (baseline {Baseline:F4} -> best {Best:F4}, churn {Churn:F3}).",
            resource.Id, result.BaselineScalar, result.BestFitness, churn);

        // Best effort: the candidate exists either way, and the list recovers on the next load.
        try
        {
            await _notificationService.NotifyWizard4CandidatesChanged(new Wizard4CandidateNotificationDto(
                resource.Id, groupId, periodFrom, periodUntil, Wizard4LifecycleConstants.ChangeKindCreated));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wizard4 candidate {ScenarioId} created but the push failed", resource.Id);
        }

        return resource;
    }

    /// <summary>
    /// Best-effort Wizard-4 run-protocol capture for the (deferred) preference-learner. Reuses the composite
    /// sub-score JSON and churn already computed for the AnalyseScenario. The created work ids are the
    /// scenario-clone works this materialise produced (AnalyseToken = scenario token), so the measurement job
    /// joins on the scenario's proposal set. A capture failure must never break the run.
    /// </summary>
    private async Task CaptureRunAsync(
        Guid jobId,
        AnalyseScenarioResource resource,
        Guid? runGroupId,
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
                Engine = WizardEngine.Wizard4,
                JobId = jobId,
                RunGroupId = runGroupId,
                ScenarioId = resource.Id,
                ApplyKind = WizardApplyKind.Scenario,
                GroupId = resource.GroupId,
                PeriodFrom = resource.FromDate,
                PeriodUntil = resource.UntilDate,
                SubScoreJson = subScoreJson,
                Stage0Violations = stage0Violations,
                ChurnAtApply = churnAtApply,
            };

            await _captureRepository.AddAsync(capture, createdWorkIds, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WizardRunCapture write failed for Wizard4 scenario {ScenarioId}; run itself is unaffected", resource.Id);
        }
    }
}
