// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
    private const string SystemActor = "wizard4";

    private readonly IHarmonizerContextBuilder _harmonizerContextBuilder;
    private readonly IWizardContextBuilder _wizardContextBuilder;
    private readonly IWizard4OptimizationCore _core;
    private readonly HarmonizerResultCache _resultCache;
    private readonly IHarmonizerApplyService _applyService;
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IWizardRunCaptureRepository _captureRepository;
    private readonly IUnitOfWork _unitOfWork;
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
        // W4 optimises the real/accepted plan (AnalyseToken == null). The in-memory bitmap built here
        // is the immutable snapshot for this pass; the candidate is created from it at the end.
        var bitmapInput = await _harmonizerContextBuilder.BuildContextAsync(
            new HarmonizerContextRequest(periodFrom, periodUntil, agentIds, AnalyseToken: null), ct);
        var objectiveContext = await _wizardContextBuilder.BuildContextAsync(
            new WizardContextRequest(periodFrom, periodUntil, agentIds, ShiftIds: null, AnalyseToken: null), ct);

        var seed = RowSorter.Sort(BitmapBuilder.Build(bitmapInput));
        var validator = new DomainAwareReplaceValidator(
            bitmapInput.Availability, bitmapInput.BoundaryAssignments, bitmapInput.IneligibleAssignments);
        var config = new HarmonizerEvolutionConfig(MaxRuntime: budget);

        var result = _core.Optimize(seed, objectiveContext, validator, config, hints: bitmapInput.SofteningHints, ct: ct);

        return await MaterializeCandidateIfImprovedAsync(result, seed, groupId, ct);
    }

    /// <summary>
    /// Public for unit testing: decides whether the run beat the snapshot by the improvement threshold
    /// and, if so, materialises the candidate scenario and captures the score snapshot.
    /// </summary>
    public async Task<AnalyseScenarioResource?> MaterializeCandidateIfImprovedAsync(
        Wizard4OptimizationResult result,
        HarmonyBitmap seed,
        Guid? groupId,
        CancellationToken ct)
    {
        if (result.BestFitness <= result.BaselineScalar + Wizard4Constants.MinImprovement)
        {
            _logger.LogInformation(
                "Wizard4 found no improvement beyond threshold (baseline {Baseline:F4}, best {Best:F4}); no candidate created.",
                result.BaselineScalar, result.BestFitness);
            return null;
        }

        var jobId = Guid.NewGuid();
        _resultCache.Store(jobId, seed, result.BestBitmap, sourceAnalyseToken: null);
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
            var subScoreJson = ScenarioScoreSerializer.Serialize(result.Best);
            var stage0Violations = result.Best.Gate.MandatoryQualMissing + result.Best.Gate.Legality;
            scenario.SubScoreJson = subScoreJson;
            scenario.ChurnRatio = churn;
            scenario.Stage0Violations = stage0Violations;
            scenario.CreatedByUser = SystemActor;
            await _scenarioRepository.Put(scenario);
            await _unitOfWork.CompleteAsync();

            await CaptureRunAsync(jobId, resource, scenario.RunGroupId, subScoreJson, stage0Violations, churn, createdIds, ct);
        }

        _logger.LogInformation(
            "Wizard4 created candidate scenario {ScenarioId} (baseline {Baseline:F4} -> best {Best:F4}, churn {Churn:F3}).",
            resource.Id, result.BaselineScalar, result.BestFitness, churn);

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
