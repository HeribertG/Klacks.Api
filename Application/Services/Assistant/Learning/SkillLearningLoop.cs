// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One pass of the learning loop. It claims the loudest ready clusters, asks once - for all of them
/// together - what kind of gap each is, and then tries to close the ones a phrase can close. Everything
/// costly is ordered so that the cheap answer comes first: the toolset the utterance already produces is
/// assembled before any model is asked, and a cluster whose target is already reachable is dismissed
/// there and then, without a single generated phrase.
/// Failure is per cluster, never per run: an exception or an unusable model answer sends that one cluster
/// back to ready with the reason recorded, and the run continues. Only a cluster that survived its
/// attempt budget becomes unfulfillable, so an outage cannot quietly declare wishes unservable - the
/// attempt counter is raised for a real failed attempt, not for a missing model.
/// </summary>
/// <param name="clusterRepository">Claims, finishes and releases clusters</param>
/// <param name="caseRepository">Supplies the evidence behind a cluster</param>
/// <param name="generator">Classifies the claimed clusters</param>
/// <param name="routingOracle">Assembles the current toolset per cluster</param>
/// <param name="phraseLearner">Runs one phrase round for a phrase gap</param>
/// <param name="capabilityLearner">Runs one composition round for a wish several skills could serve together</param>
/// <param name="descriptionSharpener">Applies the pending description proposals behind the same gate</param>
/// <param name="logger">One summary line per run</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class SkillLearningLoop : ISkillLearningLoop
{
    private const int InstanceNameMaxLength = 64;

    private static readonly IReadOnlyList<string> ReadyStatus = [SkillLearningClusterStatuses.Ready];

    private readonly ISkillLearningClusterRepository _clusterRepository;
    private readonly ISkillLearningCaseRepository _caseRepository;
    private readonly ILearnedArtifactGenerator _generator;
    private readonly ISkillRoutingOracle _routingOracle;
    private readonly IPhraseLearner _phraseLearner;
    private readonly ICapabilityLearner _capabilityLearner;
    private readonly ISkillDescriptionSharpener _descriptionSharpener;
    private readonly ILogger<SkillLearningLoop> _logger;

    public SkillLearningLoop(
        ISkillLearningClusterRepository clusterRepository,
        ISkillLearningCaseRepository caseRepository,
        ILearnedArtifactGenerator generator,
        ISkillRoutingOracle routingOracle,
        IPhraseLearner phraseLearner,
        ICapabilityLearner capabilityLearner,
        ISkillDescriptionSharpener descriptionSharpener,
        ILogger<SkillLearningLoop> logger)
    {
        _clusterRepository = clusterRepository;
        _caseRepository = caseRepository;
        _generator = generator;
        _routingOracle = routingOracle;
        _phraseLearner = phraseLearner;
        _capabilityLearner = capabilityLearner;
        _descriptionSharpener = descriptionSharpener;
        _logger = logger;
    }

    public async Task<SkillLearningRunSummary> RunAsync(CancellationToken cancellationToken = default)
    {
        await _clusterRepository.ReleaseStaleClaimsAsync(
            DateTime.UtcNow.AddMinutes(-SkillLearningDefaults.StaleClaimMinutes), cancellationToken);

        var claimed = await ClaimAsync(cancellationToken);
        var triage = await TriageAsync(claimed, cancellationToken);

        var learned = 0;
        var alreadyRouted = triage.AlreadyRouted;
        var unfulfillable = 0;
        var failed = 0;

        if (triage.Pending.Count > 0)
        {
            var classifications = await TryClassifyAsync(triage.Pending, cancellationToken);

            if (classifications == null)
            {
                failed = triage.Pending.Count;
            }
            else
            {
                foreach (var input in triage.Pending)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var outcome = await HandleClusterAsync(
                        input,
                        classifications.FirstOrDefault(c => c.ClusterId == input.Cluster.ClusterId),
                        cancellationToken);

                    switch (outcome)
                    {
                        case ClusterOutcome.Learned: learned++; break;
                        case ClusterOutcome.AlreadyRouted: alreadyRouted++; break;
                        case ClusterOutcome.Unfulfillable: unfulfillable++; break;
                        default: failed++; break;
                    }
                }
            }
        }

        var (sharpened, blocked) = await TrySharpenAsync(cancellationToken);

        var summary = new SkillLearningRunSummary(
            claimed.Count, learned, alreadyRouted, unfulfillable, failed, sharpened, blocked);

        _logger.LogInformation(
            "Skill learning run finished: claimed={Processed}, learned={Learned}, sharpened={Sharpened}, "
            + "blocked={Blocked}, unfulfillable={Unfulfillable}, alreadyRouted={AlreadyRouted}, failed={Failed}",
            summary.Processed, summary.Learned, summary.Sharpened, summary.Blocked,
            summary.Unfulfillable, summary.AlreadyRouted, summary.Failed);

        return summary;
    }

    // The classifier answers for the whole batch at once, so its failure is the whole batch's failure.
    // Every cluster it was asked about is handed straight back to ready with the reason recorded, because
    // a claim nobody is working on would otherwise sit there until the stale-claim sweep expires it an
    // hour later - an outage would cost an hour of learning instead of one run.
    private async Task<IReadOnlyList<SkillLearningClassification>?> TryClassifyAsync(
        IReadOnlyList<SkillLearningTriageInput> pending, CancellationToken cancellationToken)
    {
        try
        {
            return await _generator.ClassifyAsync(pending, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception, "Classification of {Count} learning cluster(s) failed", pending.Count);

            foreach (var input in pending)
            {
                await ReleaseAsync(
                    input.Cluster.ClusterId,
                    input.Cluster.AttemptCount,
                    exception.Message,
                    cancellationToken);
            }

            return null;
        }
    }

    // The sharpening is a second, independent half of the run. It must not take the phrase learning down
    // with it: the clusters are already finished at this point, and losing their outcome to an unrelated
    // failure would make the next run redo work that already succeeded.
    private async Task<(int Sharpened, int Blocked)> TrySharpenAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _descriptionSharpener.RunAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Description sharpening failed");
            return (0, 0);
        }
    }

    private async Task<IReadOnlyList<SkillLearningCluster>> ClaimAsync(CancellationToken cancellationToken)
    {
        var ready = await _clusterRepository.ListByStatusAsync(
            ReadyStatus, SkillLearningDefaults.MaxClustersPerRun, cancellationToken);

        var instance = Instance();
        var now = DateTime.UtcNow;
        var claimed = new List<SkillLearningCluster>();

        foreach (var cluster in ready)
        {
            if (await _clusterRepository.TryClaimForLearningAsync(cluster.Id, instance, now, cancellationToken))
            {
                claimed.Add(cluster);
            }
        }

        return claimed;
    }

    // The toolset is assembled before the model is asked, because it answers two questions at once: it is
    // the candidate list the classifier needs, and it is the baseline that decides whether there is a gap
    // at all. A wish whose target is already offered was never a routing gap - learning a phrase for it
    // would add noise to the index and claim credit for something retrieval already did.
    private async Task<TriageResult> TriageAsync(
        IReadOnlyList<SkillLearningCluster> claimed, CancellationToken cancellationToken)
    {
        var pending = new List<SkillLearningTriageInput>();
        var alreadyRouted = 0;

        foreach (var cluster in claimed)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var context = await BuildContextAsync(cluster, cancellationToken);
                var probe = await _routingOracle.ProbeAsync(
                    context.IntentExcerpt, context.Locale, context.ExpectedSkill ?? string.Empty, cancellationToken);

                if (probe.TargetFound)
                {
                    await DismissAsync(
                        cluster.Id, cluster.AttemptCount, context.ExpectedSkill!, cancellationToken);
                    alreadyRouted++;
                    continue;
                }

                // Two lists, two questions. What the assembler offered decides whether the wish is
                // already served; what retrieval can still reach decides what the classifier may name.
                // The union is the menu: the assembler contributes its always-on and guaranteed skills,
                // which retrieval never returns, and retrieval contributes the ranks the assembler cut
                // off - the ones a routing gap actually consists of.
                var reachable = await _routingOracle.ListReachableSkillsAsync(
                    context.IntentExcerpt, cancellationToken);

                pending.Add(new SkillLearningTriageInput(
                    context, probe.TopSkills, Union(probe.TopSkills, reachable)));
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Triage of learning cluster {ClusterId} failed", cluster.Id);
                await ReleaseAsync(cluster.Id, cluster.AttemptCount, exception.Message, cancellationToken);
            }
        }

        return new TriageResult(pending, alreadyRouted);
    }

    // Offered names first so the classifier reads the likeliest answers at the top, then what retrieval
    // reaches beyond them. Case-insensitive, because the two sources spell a skill name the same way but
    // nothing guarantees it.
    private static IReadOnlyList<string> Union(
        IReadOnlyList<string> offered, IReadOnlyList<string> reachable) =>
        [.. offered.Concat(reachable).Distinct(StringComparer.OrdinalIgnoreCase)];

    private async Task<ClusterOutcome> HandleClusterAsync(
        SkillLearningTriageInput input,
        SkillLearningClassification? classification,
        CancellationToken cancellationToken)
    {
        var cluster = input.Cluster;

        try
        {
            if (classification == null)
            {
                await ReleaseAsync(
                    cluster.ClusterId,
                    cluster.AttemptCount,
                    "The classifier returned no verdict for this wish.",
                    cancellationToken);
                return ClusterOutcome.Failed;
            }

            return classification.Kind switch
            {
                SkillLearningClassifications.NeedsCode => await GiveUpAsync(
                    cluster, classification.Reason ?? "No existing skill can serve this wish.", cancellationToken),
                SkillLearningClassifications.Composable => await LearnCapabilityAsync(
                    input, cancellationToken),
                _ => await LearnPhraseAsync(input, classification, cancellationToken)
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Learning round for cluster {ClusterId} failed", cluster.ClusterId);
            await ReleaseAsync(cluster.ClusterId, cluster.AttemptCount, exception.Message, cancellationToken);
            return ClusterOutcome.Failed;
        }
    }

    // The user's own correction outranks the model: somebody said which skill they meant, and no
    // classifier guess is better evidence than that.
    private async Task<ClusterOutcome> LearnPhraseAsync(
        SkillLearningTriageInput input,
        SkillLearningClassification classification,
        CancellationToken cancellationToken)
    {
        var cluster = input.Cluster;
        var target = cluster.ExpectedSkill ?? classification.TargetSkill;

        if (string.IsNullOrWhiteSpace(target))
        {
            return await FailRoundAsync(
                cluster, "The classifier named no existing skill for this wish.", cancellationToken);
        }

        // CandidateSkills and NOT ReachableSkills, deliberately: the question here is "is this wish
        // already served", and a skill the assembler offers is served whether or not retrieval could
        // reach others. The classifier picks from the wider list (SkillLearningTriageInput), so a target
        // can now be outside this one - which is what makes a classifier-chosen wish learnable at all.
        // Widening this check too would let the loop write phrases for skills that are already offered.
        if (input.CandidateSkills.Contains(target, StringComparer.OrdinalIgnoreCase))
        {
            await DismissAsync(cluster.ClusterId, cluster.AttemptCount, target, cancellationToken);
            return ClusterOutcome.AlreadyRouted;
        }

        var outcome = await _phraseLearner.LearnAsync(cluster, target, cancellationToken);
        if (!outcome.Learned)
        {
            return await FailRoundAsync(cluster, outcome.Error ?? "Phrase learning failed.", cancellationToken);
        }

        await _clusterRepository.FinishLearningAsync(
            cluster.ClusterId,
            SkillLearningClusterStatuses.LearnedPhrase,
            SkillLearningOutcomeKinds.Phrase,
            outcome.PhraseId!.Value.ToString(),
            lastError: null,
            cluster.AttemptCount,
            cancellationToken);

        return ClusterOutcome.Learned;
    }

    // A wish several skills could serve together. The composition is judged and, if it survives both
    // oracles, activated here and now - unlike a phrase there is no "activate and measure" for a recipe,
    // because an enabled recipe forces its steps on every instance the moment it exists.
    // A round that could not be judged at all - typically because no identity was available to run the
    // read-only steps under - hands the cluster back to ready with its attempt budget untouched. An
    // outage says nothing about whether the wish can be served.
    private async Task<ClusterOutcome> LearnCapabilityAsync(
        SkillLearningTriageInput input, CancellationToken cancellationToken)
    {
        var cluster = input.Cluster;

        // The narrow list on purpose. These names become the building blocks of a recipe that oracle O2
        // will really execute, and the capability path is the one that works end to end today. Handing it
        // the wider list would change the input of a proven path while fixing a broken one; a composition
        // out of skills the assembler does not even offer is also a worse composition, because the model
        // never sees those skills in a real turn.
        var outcome = await _capabilityLearner.LearnAsync(cluster, input.CandidateSkills, cancellationToken);

        if (outcome.Inconclusive)
        {
            await ReleaseAsync(
                cluster.ClusterId,
                cluster.AttemptCount,
                outcome.Error ?? "The execution oracle could not judge this composition.",
                cancellationToken);

            return ClusterOutcome.Failed;
        }

        if (!outcome.Learned)
        {
            return await FailRoundAsync(
                cluster, outcome.Error ?? "Capability learning failed.", cancellationToken);
        }

        await _clusterRepository.FinishLearningAsync(
            cluster.ClusterId,
            SkillLearningClusterStatuses.LearnedCapability,
            SkillLearningOutcomeKinds.Capability,
            outcome.RecipeName!,
            lastError: null,
            cluster.AttemptCount,
            cancellationToken);

        return ClusterOutcome.Learned;
    }

    private async Task<ClusterOutcome> GiveUpAsync(
        SkillLearningClusterContext cluster, string reason, CancellationToken cancellationToken)
    {
        await _clusterRepository.FinishLearningAsync(
            cluster.ClusterId,
            SkillLearningClusterStatuses.Unfulfillable,
            outcomeRefKind: null,
            outcomeRef: null,
            reason.Trim(),
            cluster.AttemptCount,
            cancellationToken);

        return ClusterOutcome.Unfulfillable;
    }

    private async Task<ClusterOutcome> FailRoundAsync(
        SkillLearningClusterContext cluster, string reason, CancellationToken cancellationToken)
    {
        var attempts = cluster.AttemptCount + 1;

        if (attempts >= SkillLearningDefaults.MaxLearningAttempts)
        {
            await _clusterRepository.FinishLearningAsync(
                cluster.ClusterId,
                SkillLearningClusterStatuses.Unfulfillable,
                outcomeRefKind: null,
                outcomeRef: null,
                reason,
                attempts,
                cancellationToken);

            return ClusterOutcome.Unfulfillable;
        }

        await _clusterRepository.FinishLearningAsync(
            cluster.ClusterId,
            SkillLearningClusterStatuses.Ready,
            outcomeRefKind: null,
            outcomeRef: null,
            reason,
            attempts,
            cancellationToken);

        return ClusterOutcome.Failed;
    }

    // An infrastructure failure must not spend an attempt: the budget exists to stop the loop from
    // grinding on a wish nobody can serve, not to declare wishes unservable while a model is unreachable.
    private async Task ReleaseAsync(
        Guid clusterId, int attemptCount, string reason, CancellationToken cancellationToken) =>
        await _clusterRepository.FinishLearningAsync(
            clusterId,
            SkillLearningClusterStatuses.Ready,
            outcomeRefKind: null,
            outcomeRef: null,
            reason,
            attemptCount,
            cancellationToken);

    private async Task DismissAsync(
        Guid clusterId, int attemptCount, string target, CancellationToken cancellationToken) =>
        await _clusterRepository.FinishLearningAsync(
            clusterId,
            SkillLearningClusterStatuses.Dismissed,
            outcomeRefKind: null,
            outcomeRef: null,
            $"Already routed: '{target}' is offered for this wish without anything being learned.",
            attemptCount,
            cancellationToken);

    private async Task<SkillLearningClusterContext> BuildContextAsync(
        SkillLearningCluster cluster, CancellationToken cancellationToken)
    {
        var cases = await _caseRepository.ListByClusterAsync(
            cluster.Id, SkillLearningDefaults.ClusterCaseSampleSize, cancellationToken);

        var expected = cases.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ExpectedSkill))?.ExpectedSkill;
        var chosen = cases.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.ChosenSkill))?.ChosenSkill;
        var tools = ParseToolNames(cases.FirstOrDefault()?.ToolsetJson);

        return new SkillLearningClusterContext(
            cluster.Id,
            cluster.IntentExcerpt,
            cluster.Locale,
            expected,
            chosen,
            tools,
            cluster.AttemptCount,
            cluster.LastError);
    }

    private static IReadOnlyList<string> ParseToolNames(string? toolsetJson)
    {
        if (string.IsNullOrWhiteSpace(toolsetJson))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(toolsetJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return document.RootElement
                .EnumerateArray()
                .Select(element => element.TryGetProperty("name", out var name) ? name.GetString() : null)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string Instance()
    {
        var name = Environment.MachineName;
        return name.Length <= InstanceNameMaxLength ? name : name[..InstanceNameMaxLength];
    }

    private enum ClusterOutcome
    {
        Learned,
        AlreadyRouted,
        Unfulfillable,
        Failed
    }

    private sealed record TriageResult(IReadOnlyList<SkillLearningTriageInput> Pending, int AlreadyRouted);
}
