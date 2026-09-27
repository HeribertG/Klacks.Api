// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides the description proposals the optimizer left pending, in the learning mode the settings name.
/// Collect never asks the optimizer for a proposal and decides nothing - no proposal, no optimizer LLM call and
/// no live mutation, the default of every installation, so no installation rewrites its own catalogue or spends
/// on a paid call; the learning cases stay collected in skill_learning_cases for later. Gate and AutoApply ask
/// the optimizer and measure a proposal on the live catalogue, because
/// the assembler reads the skill catalogue and the knowledge index rather than a candidate value: the planned
/// goldset items are replayed with the current description, the proposal is applied and the index rebuilt, the
/// golden cases and the same goldset items are replayed again, and the old description and version are put
/// back. Gate puts them back after every measurement and records gate_passed, so a change ships by release
/// after a review; AutoApply keeps a passing change live and exists for tests.
/// Gate changes the live catalogue for minutes per proposal, so it measures only on an explicitly triggered run;
/// a scheduled run in Gate measures nothing, like Collect. Before anything else every run, in every mode, puts
/// back what an interrupted run left live: a pending proposal whose proposed description is the live one - the
/// state a hard kill between applying and restoring leaves, since a proposal that passed is no longer pending -
/// is reset to its old description and the version below. That write only moves the catalogue towards its
/// reviewed state, so it also runs in Collect, for an installation switched back after an interrupted run.
/// A proposal passes only with proof of benefit: fixed minus regressed items over its train misses and the
/// holdout items of its skill must reach the settings-backed minimum, and no holdout item may regress between
/// the two replays. A proposal that does not change the description is a null proposal: it is measured and
/// stored as a calibration of the replay noise and never passes.
/// A paired replay that cannot be measured - no item answered on both sides, or none of the planned holdout
/// items - stores its metrics with the verdict not_measured and a count of such runs. When already the replay
/// with the current description answered nothing that could complete a pair, nothing is applied. The first such
/// run leaves the proposal pending, because a provider outage is transient; the run that reaches
/// SkillLearningDefaults.MaxUnmeasuredGateAttempts rejects it, so an unmeasurable proposal does not hold a slot
/// of the pending window and cost replays on every run.
/// The index is checked twice. After applying, because a sync that failed returns normally and a gate measured
/// on the old index judges the old description - the proposal then stays pending. After restoring, because a
/// description nothing measured must not stay what retrieval searches - a restore the index does not confirm,
/// or one whose write fails, aborts the run with SkillIndexNotRestoredException naming the skill. Applying and
/// measuring sit inside the try whose finally restores, and the restore does not use the run's cancellation
/// token, so a failed apply, a probe that throws half way or a cancelled run still lead to the restore.
/// The skill row is written whole, so an administrator's edit to the same skill during the gate window is
/// overwritten by the restore.
/// </summary>
/// <param name="optimizer">Turns recent wrong-skill corrections and goldset misses into pending proposals</param>
/// <param name="proposalRepository">Pending proposals, their verdicts and gate metrics</param>
/// <param name="agentSkillRepository">The skill row the description lives on</param>
/// <param name="goldenCaseRepository">The holdout golden cases the routing gate replays</param>
/// <param name="routingOracle">Runs the golden-case replay</param>
/// <param name="catalogRefresher">Rebuilds cache, registry and knowledge index after each change</param>
/// <param name="optionsProvider">Supplies the learning mode, the holdout minimum and the minimum net gain</param>
/// <param name="holdoutReplayGate">Plans and runs the paired goldset replay</param>
/// <param name="indexVerifier">Confirms which description the knowledge index holds for a skill</param>
/// <param name="logger">One line per decision</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillDescriptionSharpener : ISkillDescriptionSharpener
{
    private const int TrajectoriesToAnalyze = 30;
    private const string GoldenCaseBlockPrefix = "Blocked by the routing regression gate: ";
    private const string TargetedReplayBlockPrefix = "Blocked by the targeted holdout replay: ";
    private const string ItemSeparator = "; ";
    private const string NothingToReplayJustification =
        "Rejected without measurement: the reference eval run has no holdout item of this skill and none of the "
        + "proposal's train misses resolves any more.";

    private const string StaleDescriptionJustificationFormat =
        "Rejected without measurement: the description of skill {0} changed after this proposal was generated.";

    private const string UnmeasurableJustificationFormat =
        "Rejected without a verdict: the paired replay could not be measured in {0} gate runs (no replayed item "
        + "was answered on both sides, or none of the planned holdout items was).";

    private readonly ISkillDescriptionOptimizer _optimizer;
    private readonly IProposedSkillChangeRepository _proposalRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillLearningGoldenCaseRepository _goldenCaseRepository;
    private readonly ISkillRoutingOracle _routingOracle;
    private readonly ISkillCatalogRefresher _catalogRefresher;
    private readonly ISkillLearningOptionsProvider _optionsProvider;
    private readonly IGoldsetHoldoutReplayGate _holdoutReplayGate;
    private readonly ISkillIndexStateVerifier _indexVerifier;
    private readonly ILogger<SkillDescriptionSharpener> _logger;

    public SkillDescriptionSharpener(
        ISkillDescriptionOptimizer optimizer,
        IProposedSkillChangeRepository proposalRepository,
        IAgentSkillRepository agentSkillRepository,
        ISkillLearningGoldenCaseRepository goldenCaseRepository,
        ISkillRoutingOracle routingOracle,
        ISkillCatalogRefresher catalogRefresher,
        ISkillLearningOptionsProvider optionsProvider,
        IGoldsetHoldoutReplayGate holdoutReplayGate,
        ISkillIndexStateVerifier indexVerifier,
        ILogger<SkillDescriptionSharpener> logger)
    {
        _optimizer = optimizer;
        _proposalRepository = proposalRepository;
        _agentSkillRepository = agentSkillRepository;
        _goldenCaseRepository = goldenCaseRepository;
        _routingOracle = routingOracle;
        _catalogRefresher = catalogRefresher;
        _optionsProvider = optionsProvider;
        _holdoutReplayGate = holdoutReplayGate;
        _indexVerifier = indexVerifier;
        _logger = logger;
    }

    public async Task<SkillDescriptionSharpenerResult> RunAsync(
        SkillLearningRunTrigger trigger, CancellationToken cancellationToken = default)
    {
        var options = await _optionsProvider.GetAsync(cancellationToken);
        await RestoreInterruptedRunAsync(cancellationToken);

        if (options.Mode == SkillLearningMode.Collect)
        {
            _logger.LogInformation(
                "Learning mode is {Mode}: no proposal is generated (no optimizer LLM call) and nothing is "
                    + "measured or applied; learning cases stay collected for later",
                options.Mode);
            return SkillDescriptionSharpenerResult.Empty;
        }

        var optimizerResult = await _optimizer.GenerateProposalsAsync(TrajectoriesToAnalyze, cancellationToken);

        if (options.Mode == SkillLearningMode.Gate && trigger == SkillLearningRunTrigger.Scheduled)
        {
            _logger.LogInformation(
                "Description proposals are only collected in learning mode {Mode} on a {Trigger} run; nothing is "
                    + "measured or applied",
                options.Mode, trigger);
            return new SkillDescriptionSharpenerResult(0, 0, optimizerResult.Attempts, optimizerResult.Failures);
        }

        var pending = await _proposalRepository.GetPendingAsync(
            ProposedChangeFields.Description, SkillLearningDefaults.MaxProposalsPerRun, cancellationToken);

        if (pending.Count == 0)
        {
            return new SkillDescriptionSharpenerResult(0, 0, optimizerResult.Attempts, optimizerResult.Failures);
        }

        var holdoutCount = await _goldenCaseRepository.CountHoldoutAsync(cancellationToken);
        if (holdoutCount < options.MinGoldenCasesForAutoApply)
        {
            _logger.LogInformation(
                "Description gate skipped: {Count} holdout golden case(s), {Minimum} required. "
                    + "{Pending} proposal(s) stay pending for a person to decide",
                holdoutCount, options.MinGoldenCasesForAutoApply, pending.Count);
            return new SkillDescriptionSharpenerResult(0, 0, optimizerResult.Attempts, optimizerResult.Failures);
        }

        var passed = 0;
        var blocked = 0;
        GatePopulation? population = null;

        foreach (var proposal in pending)
        {
            cancellationToken.ThrowIfCancellationRequested();

            population = await ResolveGatePopulationAsync(proposal, population, cancellationToken);

            var decision = await DecideAsync(proposal, population, options, cancellationToken);
            if (decision == null)
            {
                continue;
            }

            population = population with { Baseline = decision.Baseline };

            if (decision.Status is ProposedChangeStatuses.AppliedAuto or ProposedChangeStatuses.GatePassed)
            {
                passed++;
            }
            else if (decision.Status == ProposedChangeStatuses.BlockedRegression)
            {
                blocked++;
            }
        }

        return new SkillDescriptionSharpenerResult(passed, blocked, optimizerResult.Attempts, optimizerResult.Failures);
    }

    // Runs before any golden-case baseline is measured, because a baseline taken with the leaked candidate live
    // would judge the proposal against itself.
    private async Task RestoreInterruptedRunAsync(CancellationToken cancellationToken)
    {
        var pending = await _proposalRepository.GetPendingAsync(
            ProposedChangeFields.Description,
            SkillLearningDefaults.MaxPendingProposalsCheckedForInterruptedGate,
            cancellationToken);

        foreach (var proposal in pending)
        {
            if (string.Equals(proposal.ValueAfter, proposal.ValueBefore, StringComparison.Ordinal))
            {
                continue;
            }

            var skill = await _agentSkillRepository.GetByIdAsync(proposal.SkillId, cancellationToken);
            if (skill == null || !string.Equals(skill.Description, proposal.ValueAfter, StringComparison.Ordinal))
            {
                continue;
            }

            _logger.LogWarning(
                "The never-judged description of proposal {ProposalId} is live on skill {Name}, left by an "
                    + "interrupted gate run; it is put back to the description the proposal was written against",
                proposal.Id, skill.Name);
            await RestoreOrAbortAsync(skill, proposal.ValueBefore, skill.Version - 1, proposal);
        }
    }

    // The golden-case budget is spent on the holdout cases of the skill the proposal changes. Two proposals for
    // the same skill share one baseline pass; a different skill needs its own, because a baseline measured on
    // other cases would report their failures as this proposal's doing.
    private async Task<GatePopulation> ResolveGatePopulationAsync(
        ProposedSkillChange proposal, GatePopulation? current, CancellationToken cancellationToken)
    {
        if (current != null && string.Equals(current.SkillName, proposal.SkillName, StringComparison.Ordinal))
        {
            return current;
        }

        var goldenCases = await _goldenCaseRepository.ListHoldoutAsync(
            SkillLearningDefaults.MaxGoldenCasesPerRegressionCheck, proposal.SkillName, cancellationToken);
        var baseline = await _routingOracle.FindFailingGoldenCasesAsync(goldenCases, cancellationToken);

        return new GatePopulation(proposal.SkillName, goldenCases, baseline);
    }

    private async Task<Decision?> DecideAsync(
        ProposedSkillChange proposal,
        GatePopulation population,
        SkillLearningOptions options,
        CancellationToken cancellationToken)
    {
        if (proposal.Field != ProposedChangeFields.Description)
        {
            return null;
        }

        var skill = await _agentSkillRepository.GetByIdAsync(proposal.SkillId, cancellationToken);
        if (skill == null)
        {
            return null;
        }

        var original = skill.Description;
        var originalVersion = skill.Version;
        if (!string.Equals(original, proposal.ValueBefore, StringComparison.Ordinal))
        {
            proposal.Justification = string.Format(
                CultureInfo.InvariantCulture, StaleDescriptionJustificationFormat, skill.Name);
            await MarkAsync(proposal, ProposedChangeStatuses.Rejected, cancellationToken);
            _logger.LogInformation(
                "Proposal {ProposalId} for skill {Name} rejected: the description changed since it was generated",
                proposal.Id, skill.Name);
            return new Decision(ProposedChangeStatuses.Rejected, population.Baseline);
        }

        var evidence = GoldsetMissEvidenceCodec.Parse(proposal.EvidenceJson);
        var plan = await _holdoutReplayGate.PlanAsync(skill.Name, evidence.Items, cancellationToken);
        if (plan == null)
        {
            _logger.LogInformation(
                "Proposal {ProposalId} for skill {Name} cannot be measured yet (no reference run); it stays pending",
                proposal.Id, skill.Name);
            return null;
        }

        // Permanent, unlike a missing reference run: left pending, such a proposal would hold one of the few
        // per-run slots for good and block every later proposal for its skill.
        if (plan.HoldoutItems.Count == 0 && plan.TrainItems.Count == 0)
        {
            proposal.GateMetricsJson = GoldsetGateMetricsCodec.Serialize(BuildMetrics(
                plan, null, Array.Empty<string>(), options.GateMinNetGain, false, GoldsetGateVerdicts.NotMeasured));
            proposal.Justification = NothingToReplayJustification;
            await MarkAsync(proposal, ProposedChangeStatuses.Rejected, cancellationToken);
            _logger.LogInformation(
                "Proposal {ProposalId} for skill {Name} rejected: nothing to replay against run {RunId}",
                proposal.Id, skill.Name, plan.ReferenceEvalRunId);
            return new Decision(ProposedChangeStatuses.Rejected, population.Baseline);
        }

        var isCalibration = string.Equals(proposal.ValueAfter, proposal.ValueBefore, StringComparison.Ordinal);
        var before = await _holdoutReplayGate.ReplayAsync(plan, cancellationToken);

        if (!CanCompleteAPair(plan, before))
        {
            var unmeasured = Unmeasured(
                proposal,
                plan,
                BuildMetrics(plan, null, Array.Empty<string>(), options.GateMinNetGain, isCalibration, GoldsetGateVerdicts.NotMeasured),
                population.Baseline);
            return await RecordAsync(proposal, skill.Name, unmeasured, population.Baseline, cancellationToken);
        }

        Measurement? measurement = null;
        var keepChange = false;
        try
        {
            try
            {
                skill.Description = proposal.ValueAfter;
                skill.Version = originalVersion + 1;
                await _agentSkillRepository.UpdateAsync(skill, cancellationToken);
                await _catalogRefresher.RefreshAndWaitForIndexAsync(
                    $"measuring description proposal {proposal.Id}", cancellationToken);

                measurement = await MeasureAsync(
                    skill.Name, proposal, population, plan, before, options, isCalibration, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(
                    exception,
                    "The gate could not be measured for proposal {ProposalId}; the description of skill {Name} "
                        + "was put back and the proposal stays pending",
                    proposal.Id, skill.Name);
                measurement = null;
            }

            keepChange = measurement?.Status == ProposedChangeStatuses.AppliedAuto;
        }
        finally
        {
            if (!keepChange)
            {
                await RestoreOrAbortAsync(skill, original, originalVersion, proposal);
            }
        }

        if (measurement == null)
        {
            return null;
        }

        return await RecordAsync(
            proposal, skill.Name, measurement, keepChange ? measurement.Failing : population.Baseline, cancellationToken);
    }

    // With planned holdout items the judge needs one of them answered on both sides, otherwise any train item;
    // an item the first replay did not answer can never complete a pair, so applying would be for nothing.
    private static bool CanCompleteAPair(GoldsetReplayPlan plan, IReadOnlyDictionary<GoldsetItemRef, bool?> before)
    {
        bool Answered(GoldsetItemRef item) => before.TryGetValue(item, out var hit) && hit != null;

        return plan.HoldoutItems.Count > 0
            ? plan.HoldoutItems.Any(Answered)
            : plan.TrainItems.Any(Answered);
    }

    private async Task<Measurement?> MeasureAsync(
        string skillName,
        ProposedSkillChange proposal,
        GatePopulation population,
        GoldsetReplayPlan plan,
        IReadOnlyDictionary<GoldsetItemRef, bool?> before,
        SkillLearningOptions options,
        bool isCalibration,
        CancellationToken cancellationToken)
    {
        if (!await _indexVerifier.IsIndexedAsync(skillName, proposal.ValueAfter, cancellationToken))
        {
            _logger.LogWarning(
                "The knowledge index does not show the proposed description of skill {Name}; proposal {ProposalId} "
                    + "stays pending",
                skillName, proposal.Id);
            return null;
        }

        var failing = await _routingOracle.FindFailingGoldenCasesAsync(population.GoldenCases, cancellationToken);
        var goldenRegressions = failing.Except(population.Baseline, StringComparer.Ordinal).ToList();

        if (goldenRegressions.Count > 0 && !isCalibration)
        {
            return new Measurement(
                ProposedChangeStatuses.BlockedRegression,
                GoldenCaseBlockPrefix + string.Join(ItemSeparator, goldenRegressions),
                BuildMetrics(plan, null, goldenRegressions, options.GateMinNetGain, false, GoldsetGateVerdicts.GoldenCaseRegression),
                failing);
        }

        var after = await _holdoutReplayGate.ReplayAsync(plan, cancellationToken);
        var outcome = GoldsetGateJudge.Judge(plan, before, after, options.GateMinNetGain);
        var metrics = BuildMetrics(plan, outcome, goldenRegressions, options.GateMinNetGain, isCalibration, outcome.Verdict);

        if (outcome.Verdict == GoldsetGateVerdicts.NotMeasured)
        {
            return Unmeasured(proposal, plan, metrics, failing);
        }

        if (isCalibration)
        {
            return new Measurement(ProposedChangeStatuses.Rejected, null, metrics, failing);
        }

        return outcome.Verdict switch
        {
            GoldsetGateVerdicts.BlockedRegression => new Measurement(
                ProposedChangeStatuses.BlockedRegression,
                TargetedReplayBlockPrefix + string.Join(ItemSeparator, outcome.HoldoutRegressions),
                metrics,
                failing),
            GoldsetGateVerdicts.NoNetGain => new Measurement(ProposedChangeStatuses.Rejected, null, metrics, failing),
            _ => new Measurement(
                options.Mode == SkillLearningMode.AutoApply
                    ? ProposedChangeStatuses.AppliedAuto
                    : ProposedChangeStatuses.GatePassed,
                null,
                metrics,
                failing)
        };
    }

    private Measurement Unmeasured(
        ProposedSkillChange proposal, GoldsetReplayPlan plan, GoldsetGateMetrics metrics, IReadOnlyList<string> failing)
    {
        var attempts = GoldsetGateMetricsCodec.ReadUnmeasuredAttempts(proposal.GateMetricsJson) + 1;
        var counted = metrics with { UnmeasuredAttempts = attempts };

        if (attempts >= SkillLearningDefaults.MaxUnmeasuredGateAttempts)
        {
            _logger.LogInformation(
                "Proposal {ProposalId} could not be judged in {Attempts} gate run(s); it is rejected",
                proposal.Id, attempts);
            return new Measurement(
                ProposedChangeStatuses.Rejected,
                string.Format(CultureInfo.InvariantCulture, UnmeasurableJustificationFormat, attempts),
                counted,
                failing);
        }

        _logger.LogInformation(
            "Proposal {ProposalId} could not be judged: no replayed item was answered on both sides, or none of "
                + "its {HoldoutPlanned} planned holdout item(s) was; it stays pending (attempt {Attempts} of {Maximum})",
            proposal.Id, plan.HoldoutItems.Count, attempts, SkillLearningDefaults.MaxUnmeasuredGateAttempts);
        return new Measurement(ProposedChangeStatuses.Pending, null, counted, failing);
    }

    private async Task<Decision> RecordAsync(
        ProposedSkillChange proposal,
        string skillName,
        Measurement measurement,
        IReadOnlyList<string> baseline,
        CancellationToken cancellationToken)
    {
        proposal.GateMetricsJson = GoldsetGateMetricsCodec.Serialize(measurement.Metrics);
        if (measurement.Justification != null)
        {
            proposal.Justification = measurement.Justification;
        }

        if (measurement.Status == ProposedChangeStatuses.Pending)
        {
            proposal.UpdateTime = DateTime.UtcNow;
            await _proposalRepository.UpdateAsync(proposal, cancellationToken);
        }
        else
        {
            await MarkAsync(proposal, measurement.Status, cancellationToken);
        }

        _logger.LogInformation(
            "Description proposal {ProposalId} for skill {Name}: {Status} ({Verdict}, net gain {NetGain})",
            proposal.Id, skillName, measurement.Status, measurement.Metrics.Verdict, measurement.Metrics.NetGain);

        return new Decision(measurement.Status, baseline);
    }

    // The learning loop lets only SkillIndexNotRestoredException end a run, so a write-back that fails in the
    // database is wrapped in it: it leaves the never-judged description live just as surely as an index that
    // does not confirm the restore.
    private async Task RestoreOrAbortAsync(
        AgentSkill skill, string description, int version, ProposedSkillChange proposal)
    {
        try
        {
            await RestoreAsync(skill, description, version, proposal.Id);
        }
        catch (Exception restoreException) when (restoreException is not OperationCanceledException)
        {
            _logger.LogError(
                restoreException,
                "Putting back the description of skill {SkillId} ({SkillName}) failed: the never-judged "
                    + "description '{NewDescription}' may still be live instead of '{OldDescription}'",
                skill.Id, skill.Name, proposal.ValueAfter, description);

            if (restoreException is SkillIndexNotRestoredException)
            {
                throw;
            }

            throw new SkillIndexNotRestoredException(skill.Name, restoreException);
        }
    }

    // The restore is the one write that has to land even when the run was cancelled, so it does not take the
    // run's token.
    private async Task RestoreAsync(AgentSkill skill, string description, int version, Guid proposalId)
    {
        skill.Description = description;
        skill.Version = version;
        await _agentSkillRepository.UpdateAsync(skill, CancellationToken.None);
        await _catalogRefresher.RefreshAndWaitForIndexAsync(
            $"restoring description after proposal {proposalId}", CancellationToken.None);

        if (!await _indexVerifier.IsIndexedAsync(skill.Name, description, CancellationToken.None))
        {
            throw new SkillIndexNotRestoredException(skill.Name);
        }
    }

    private async Task MarkAsync(ProposedSkillChange proposal, string status, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        proposal.Status = status;
        proposal.ReviewedBy = SkillLearningDefaults.AutomaticReviewer;
        proposal.ReviewedAt = now;
        proposal.UpdateTime = now;
        await _proposalRepository.UpdateAsync(proposal, cancellationToken);
    }

    private static GoldsetGateMetrics BuildMetrics(
        GoldsetReplayPlan plan,
        GoldsetGateOutcome? outcome,
        IReadOnlyList<string> goldenRegressions,
        int minNetGain,
        bool isCalibration,
        string verdict) =>
        new(
            plan.ReferenceEvalRunId,
            plan.Model,
            plan.ScorerVersion,
            plan.HoldoutItems.Count,
            outcome?.HoldoutMeasured ?? 0,
            outcome?.HoldoutRegressions ?? Array.Empty<string>(),
            outcome?.HoldoutFixed ?? Array.Empty<string>(),
            plan.TrainItems.Count,
            outcome?.TrainMeasured ?? 0,
            outcome?.TrainFixed ?? Array.Empty<string>(),
            outcome?.TrainRegressions ?? Array.Empty<string>(),
            goldenRegressions,
            outcome?.NetGain ?? 0,
            minNetGain,
            verdict,
            isCalibration,
            DateTime.UtcNow);

    private sealed record Decision(string Status, IReadOnlyList<string> Baseline);

    private sealed record Measurement(
        string Status, string? Justification, GoldsetGateMetrics Metrics, IReadOnlyList<string> Failing);

    private sealed record GatePopulation(
        string SkillName,
        IReadOnlyList<SkillLearningGoldenCase> GoldenCases,
        IReadOnlyList<string> Baseline);
}
