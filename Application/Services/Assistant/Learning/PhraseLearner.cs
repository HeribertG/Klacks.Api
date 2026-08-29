// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One round of phrase learning. The generator proposes wordings, and each is judged the only way a
/// wording can honestly be judged: it is written into skill_phrase, the catalogue is rebuilt, and the
/// routing oracle is asked whether the original utterance now reaches the target skill. A wording that
/// does not earn that, or that breaks a golden case in earning it, is rolled back to Rejected before the
/// next one is tried - so at most one learned phrase per round survives, and a failed round leaves the
/// index exactly as it found it.
/// Rejected rather than deleted on rollback: the row keeps the unique key occupied, which is what stops a
/// later round from proposing the identical wording again.
/// </summary>
/// <param name="agentRepository">Supplies the default agent the target skill belongs to</param>
/// <param name="agentSkillRepository">Resolves the target skill and its description</param>
/// <param name="phraseRepository">Writes and withdraws the learned phrase</param>
/// <param name="candidateRepository">Records every variant and its verdict</param>
/// <param name="goldenCaseRepository">The regression goldset, and where a success is frozen</param>
/// <param name="generator">Produces the wordings</param>
/// <param name="routingOracle">Decides whether a wording worked</param>
/// <param name="catalogRefresher">Rebuilds cache, registry and knowledge index between the attempts</param>
/// <param name="logger">One line per activated or rejected variant</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class PhraseLearner : IPhraseLearner
{
    private const string ActivationReason = "learned phrase activated";
    private const string RollbackReason = "learned phrase withdrawn after a failed routing probe";

    private readonly IAgentRepository _agentRepository;
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly ISkillLearningCandidateRepository _candidateRepository;
    private readonly ISkillLearningGoldenCaseRepository _goldenCaseRepository;
    private readonly ILearnedArtifactGenerator _generator;
    private readonly ISkillRoutingOracle _routingOracle;
    private readonly ISkillCatalogRefresher _catalogRefresher;
    private readonly ILogger<PhraseLearner> _logger;

    public PhraseLearner(
        IAgentRepository agentRepository,
        IAgentSkillRepository agentSkillRepository,
        ISkillPhraseRepository phraseRepository,
        ISkillLearningCandidateRepository candidateRepository,
        ISkillLearningGoldenCaseRepository goldenCaseRepository,
        ILearnedArtifactGenerator generator,
        ISkillRoutingOracle routingOracle,
        ISkillCatalogRefresher catalogRefresher,
        ILogger<PhraseLearner> logger)
    {
        _agentRepository = agentRepository;
        _agentSkillRepository = agentSkillRepository;
        _phraseRepository = phraseRepository;
        _candidateRepository = candidateRepository;
        _goldenCaseRepository = goldenCaseRepository;
        _generator = generator;
        _routingOracle = routingOracle;
        _catalogRefresher = catalogRefresher;
        _logger = logger;
    }

    public async Task<PhraseLearningOutcome> LearnAsync(
        SkillLearningClusterContext cluster,
        string targetSkill,
        CancellationToken cancellationToken = default)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return PhraseLearningOutcome.Failure("No default agent is configured.");
        }

        var skill = await _agentSkillRepository.GetByNameAsync(agent.Id, targetSkill, cancellationToken);
        if (skill == null)
        {
            return PhraseLearningOutcome.Failure($"Skill '{targetSkill}' does not exist.");
        }

        var language = ResolveLanguage(cluster.Locale);

        var existing = await _phraseRepository.GetPhraseTextsAsync(
            SkillPhraseOwnerKinds.Skill,
            skill.Name,
            language,
            SkillLearningDefaults.GeneratorExistingPhraseSamples,
            cancellationToken);

        var phrases = await _generator.GeneratePhrasesAsync(
            cluster, skill.Name, skill.Description, existing, cluster.LastError, cancellationToken);

        if (phrases.Count == 0)
        {
            return PhraseLearningOutcome.Failure("The generator produced no usable phrase.");
        }

        var goldenCases = await _goldenCaseRepository.ListAsync(
            SkillLearningDefaults.MaxGoldenCasesPerRegressionCheck, cancellationToken);
        var baseline = await _routingOracle.FindFailingGoldenCasesAsync(goldenCases, cancellationToken);

        var variantNo = await _candidateRepository.CountByClusterAsync(cluster.ClusterId, cancellationToken);
        string lastError = "No generated phrase made the target reachable.";

        foreach (var phrase in phrases)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attempt = await TryVariantAsync(
                cluster, skill.Name, language, phrase, variantNo++, goldenCases, baseline, cancellationToken);

            if (attempt.Learned)
            {
                await FreezeGoldenCaseAsync(cluster, skill.Name, cancellationToken);
                return attempt;
            }

            lastError = attempt.Error ?? lastError;
        }

        return PhraseLearningOutcome.Failure(lastError);
    }

    private async Task<PhraseLearningOutcome> TryVariantAsync(
        SkillLearningClusterContext cluster,
        string skillName,
        string language,
        string phrase,
        int variantNo,
        IReadOnlyList<SkillLearningGoldenCase> goldenCases,
        IReadOnlyList<string> baseline,
        CancellationToken cancellationToken)
    {
        var candidate = new SkillLearningCandidate
        {
            Id = Guid.NewGuid(),
            ClusterId = cluster.ClusterId,
            VariantNo = variantNo,
            Kind = SkillLearningCandidateKinds.Phrase,
            PayloadJson = JsonSerializer.Serialize(new { ownerName = skillName, language, phrase }),
            Status = SkillLearningCandidateStatuses.Generated
        };

        await _candidateRepository.AddAsync(candidate, cancellationToken);

        var phraseId = await _phraseRepository.TryAddLearnedAsync(
            SkillPhraseOwnerKinds.Skill, skillName, language, SkillPhraseKinds.Synonym, phrase, cancellationToken);

        if (phraseId == null)
        {
            var duplicate = $"The wording '{phrase}' is already indexed for '{skillName}'.";
            await _candidateRepository.UpdateVerdictAsync(
                candidate.Id, SkillLearningCandidateStatuses.RoutingFailed, null, null, duplicate, null, cancellationToken);
            return PhraseLearningOutcome.Failure(duplicate);
        }

        // The refresher swallows a failing index sync by design, so a probe can run against an index that
        // does not contain the new phrase yet. That error is one-directional: the phrase looks useless and
        // is withdrawn, never useful when it is not, and the cluster comes back for another round.
        await _catalogRefresher.RefreshAsync(ActivationReason, cancellationToken);

        var excerptProbe = await _routingOracle.ProbeAsync(
            cluster.IntentExcerpt, cluster.Locale, skillName, cancellationToken);
        var phraseProbe = await _routingOracle.ProbeAsync(phrase, cluster.Locale, skillName, cancellationToken);

        var failure = await JudgeAsync(
            skillName, excerptProbe, phraseProbe, goldenCases, baseline, cancellationToken);

        var routingJson = JsonSerializer.Serialize(new
        {
            excerptFound = excerptProbe.TargetFound,
            phraseFound = phraseProbe.TargetFound,
            offered = excerptProbe.TopSkills
        });

        if (failure == null)
        {
            await _candidateRepository.UpdateVerdictAsync(
                candidate.Id, SkillLearningCandidateStatuses.Active, routingJson, null, null, DateTime.UtcNow,
                cancellationToken);

            _logger.LogInformation(
                "Learned phrase '{Phrase}' ({Language}) for skill {Skill}", phrase, language, skillName);

            return PhraseLearningOutcome.Success(phraseId.Value, phrase);
        }

        await _phraseRepository.SetStatusAsync(phraseId.Value, SkillPhraseStatuses.Rejected, cancellationToken);
        await _catalogRefresher.RefreshAsync(RollbackReason, cancellationToken);

        await _candidateRepository.UpdateVerdictAsync(
            candidate.Id, SkillLearningCandidateStatuses.RoutingFailed, routingJson, null, failure, null,
            cancellationToken);

        _logger.LogInformation(
            "Rejected phrase '{Phrase}' for skill {Skill}: {Reason}", phrase, skillName, failure);

        return PhraseLearningOutcome.Failure(failure);
    }

    // The regression replay only runs once the phrase actually helped: it is by far the most expensive
    // step, and a wording that did not even reach its own target is rejected either way.
    private async Task<string?> JudgeAsync(
        string skillName,
        SkillRoutingProbe excerptProbe,
        SkillRoutingProbe phraseProbe,
        IReadOnlyList<SkillLearningGoldenCase> goldenCases,
        IReadOnlyList<string> baseline,
        CancellationToken cancellationToken)
    {
        if (!excerptProbe.TargetFound)
        {
            return $"'{skillName}' is still not offered for the original wish; offered instead: "
                + Describe(excerptProbe.TopSkills);
        }

        if (!phraseProbe.TargetFound)
        {
            return $"'{skillName}' is not offered for the phrase itself; offered instead: "
                + Describe(phraseProbe.TopSkills);
        }

        var failing = await _routingOracle.FindFailingGoldenCasesAsync(goldenCases, cancellationToken);
        var regressions = failing.Except(baseline, StringComparer.Ordinal).ToList();

        return regressions.Count == 0
            ? null
            : "It would break earlier learning: " + string.Join("; ", regressions);
    }

    private async Task FreezeGoldenCaseAsync(
        SkillLearningClusterContext cluster, string skillName, CancellationToken cancellationToken)
    {
        if (await _goldenCaseRepository.ExistsAsync(cluster.IntentExcerpt, skillName, cancellationToken))
        {
            return;
        }

        await _goldenCaseRepository.AddAsync(
            new SkillLearningGoldenCase
            {
                Id = Guid.NewGuid(),
                Query = cluster.IntentExcerpt,
                Locale = cluster.Locale,
                ExpectedSourceId = skillName,
                ClusterId = cluster.ClusterId
            },
            cancellationToken);
    }

    private static string Describe(IReadOnlyList<string> names) =>
        names.Count == 0 ? "nothing" : string.Join(", ", names.Take(SkillLearningDefaults.PhraseVariantsPerRound));

    // skill_phrase stores one language tag per row, so a regional locale is folded onto its base language:
    // a phrase learned from a de-CH utterance is a German phrase. An unknown locale becomes the reserved
    // "undetermined" tag rather than a guess, which is also where the grouper truncates first when the
    // index text hits its token cap.
    private static string ResolveLanguage(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return SkillPhraseLanguages.Undetermined;
        }

        var trimmed = locale.Trim();
        var separator = trimmed.IndexOfAny(['-', '_']);
        var baseTag = separator > 0 ? trimmed[..separator] : trimmed;

        return baseTag.Length == 2 && baseTag.All(char.IsLetter)
            ? baseTag.ToLowerInvariant()
            : SkillPhraseLanguages.Undetermined;
    }
}
