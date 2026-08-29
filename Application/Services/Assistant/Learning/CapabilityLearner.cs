// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One round of capability learning. The order of the judgements is the whole design: everything that
/// can be decided without touching the database is decided first, and the recipe is written only once it
/// has already passed. That is not an optimisation. A recipe forces its step skill deterministically,
/// ahead of function calling, and the engine reads the table on every call without a cache - the moment
/// an enabled row exists it is live on every instance. There is no equivalent of the phrase learner's
/// "activate, measure, withdraw" here, because a bad recipe would be stealing real turns while being
/// measured.
/// So: the validator rules out hijacking, then the execution oracle proves the steps, and only then is
/// the row written. The single check that genuinely needs the row - that the live engine, with all
/// recipes in their real order, resolves this wish to this recipe - runs last, and its failure removes
/// the row again.
/// A round that could not be judged is reported as such rather than as a failure: an unavailable
/// identity says nothing about whether the wish can be served.
/// </summary>
/// <param name="recipeRepository">Reads the recipes the new trigger must stay disjoint from, and writes the accepted one</param>
/// <param name="phraseRepository">Writes the recipe's trigger words so the semantic fallback can find it too</param>
/// <param name="candidateRepository">Records every variant and its verdict</param>
/// <param name="caseRepository">Supplies the user whose identity the read-only steps run under</param>
/// <param name="goldenCaseRepository">Routing expectations the new trigger must not swallow</param>
/// <param name="generator">Composes the variants</param>
/// <param name="validator">Rules out unusable and hijacking triggers before anything is written</param>
/// <param name="executionOracle">Oracle O2</param>
/// <param name="recipeEngine">The live engine, asked whether the activated recipe really wins the wish</param>
/// <param name="registry">Resolves the candidate skill names to descriptors</param>
/// <param name="riskClassifier">Decides which candidates may be offered as building blocks at all</param>
/// <param name="catalogRefresher">Rebuilds cache, registry and knowledge index around the activation</param>
/// <param name="logger">One line per activated or rejected variant</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class CapabilityLearner : ICapabilityLearner
{
    private const string ActivationReason = "learned capability activated";
    private const string RollbackReason = "learned capability withdrawn after a failed routing probe";
    private const int ExampleRecipeCount = 2;

    // The routing probe runs with administrator rights, unlike the execution probe, which deliberately
    // runs as the wishing user. The two ask different questions: whether the engine picks this recipe
    // for this wording is a property of the recipe corpus and must not change with who asked last,
    // whereas whether a step actually works is precisely a question about that user's permissions.
    // Same reasoning as SkillRoutingOracle, which probes retrieval the same way.
    private static readonly List<string> ProbeRights = [Roles.Admin];

    private static readonly JsonSerializerOptions RecipeJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly ISkillLearningCandidateRepository _candidateRepository;
    private readonly ISkillLearningCaseRepository _caseRepository;
    private readonly ISkillLearningGoldenCaseRepository _goldenCaseRepository;
    private readonly ILearnedArtifactGenerator _generator;
    private readonly IRecipeDraftValidator _validator;
    private readonly ISkillExecutionOracle _executionOracle;
    private readonly RecipeEngineService _recipeEngine;
    private readonly ISkillRegistry _registry;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly ISkillCatalogRefresher _catalogRefresher;
    private readonly ILogger<CapabilityLearner> _logger;

    public CapabilityLearner(
        IAgentRecipeRepository recipeRepository,
        ISkillPhraseRepository phraseRepository,
        ISkillLearningCandidateRepository candidateRepository,
        ISkillLearningCaseRepository caseRepository,
        ISkillLearningGoldenCaseRepository goldenCaseRepository,
        ILearnedArtifactGenerator generator,
        IRecipeDraftValidator validator,
        ISkillExecutionOracle executionOracle,
        RecipeEngineService recipeEngine,
        ISkillRegistry registry,
        ISkillRiskClassifier riskClassifier,
        ISkillCatalogRefresher catalogRefresher,
        ILogger<CapabilityLearner> logger)
    {
        _recipeRepository = recipeRepository;
        _phraseRepository = phraseRepository;
        _candidateRepository = candidateRepository;
        _caseRepository = caseRepository;
        _goldenCaseRepository = goldenCaseRepository;
        _generator = generator;
        _validator = validator;
        _executionOracle = executionOracle;
        _recipeEngine = recipeEngine;
        _registry = registry;
        _riskClassifier = riskClassifier;
        _catalogRefresher = catalogRefresher;
        _logger = logger;
    }

    public async Task<CapabilityLearningOutcome> LearnAsync(
        SkillLearningClusterContext cluster,
        IReadOnlyList<string> candidateSkills,
        CancellationToken cancellationToken = default)
    {
        var blocks = BuildBlocks(candidateSkills);
        if (blocks.Count == 0)
        {
            return CapabilityLearningOutcome.Failure(
                "None of the skills offered for this wish may be composed into a capability.");
        }

        var existing = await _recipeRepository.GetAllEnabledAsync(cancellationToken);
        var goldenCases = await _goldenCaseRepository.ListAsync(
            SkillLearningDefaults.MaxGoldenCasesPerRegressionCheck, cancellationToken);

        var drafts = await _generator.GenerateCapabilitiesAsync(
            cluster, blocks, BuildExamples(existing), cluster.LastError, cancellationToken);

        if (drafts.Count == 0)
        {
            return CapabilityLearningOutcome.Failure("The generator produced no usable capability.");
        }

        var ownerUserId = await ResolveOwnerAsync(cluster.ClusterId, cancellationToken);
        var variantNo = await _candidateRepository.CountByClusterAsync(cluster.ClusterId, cancellationToken);
        var lastError = "No composed capability survived the oracles.";

        foreach (var draft in drafts)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var attempt = await TryVariantAsync(
                cluster, draft, existing, goldenCases, ownerUserId, variantNo++, cancellationToken);

            if (attempt.Learned || attempt.Inconclusive)
            {
                return attempt;
            }

            lastError = attempt.Error ?? lastError;
        }

        return CapabilityLearningOutcome.Failure(lastError);
    }

    private async Task<CapabilityLearningOutcome> TryVariantAsync(
        SkillLearningClusterContext cluster,
        LearnedRecipeDraft draft,
        IReadOnlyList<AgentRecipe> existing,
        IReadOnlyList<SkillLearningGoldenCase> goldenCases,
        string? ownerUserId,
        int variantNo,
        CancellationToken cancellationToken)
    {
        var candidate = new SkillLearningCandidate
        {
            Id = Guid.NewGuid(),
            ClusterId = cluster.ClusterId,
            VariantNo = variantNo,
            Kind = SkillLearningCandidateKinds.Capability,
            PayloadJson = Serialize(draft),
            Status = SkillLearningCandidateStatuses.Generated
        };

        await _candidateRepository.AddAsync(candidate, cancellationToken);

        var verdict = _validator.Validate(draft, existing, goldenCases);
        if (!verdict.IsAccepted)
        {
            await FailAsync(
                candidate.Id, SkillLearningCandidateStatuses.RoutingFailed, null, verdict.Error!, cancellationToken);
            return CapabilityLearningOutcome.Failure(verdict.Error!);
        }

        var probe = await _executionOracle.ProbeAsync(
            draft.Steps, ownerUserId, cluster.ClusterId, cancellationToken);
        var probeJson = JsonSerializer.Serialize(probe, RecipeJsonOptions);

        if (probe.Verdict == SkillExecutionVerdict.Inconclusive)
        {
            await FailAsync(
                candidate.Id, SkillLearningCandidateStatuses.Generated, probeJson, probe.Error, cancellationToken);
            return CapabilityLearningOutcome.Unjudged(probe.Error ?? "The execution oracle could not run.");
        }

        if (probe.Verdict == SkillExecutionVerdict.Rejected)
        {
            await FailAsync(
                candidate.Id, SkillLearningCandidateStatuses.ExecutionFailed, probeJson, probe.Error!, cancellationToken);
            return CapabilityLearningOutcome.Failure(probe.Error!);
        }

        return await ActivateAsync(
            cluster, draft, verdict, candidate.Id, probe, probeJson, cancellationToken);
    }

    private async Task<CapabilityLearningOutcome> ActivateAsync(
        SkillLearningClusterContext cluster,
        LearnedRecipeDraft draft,
        RecipeDraftVerdict verdict,
        Guid candidateId,
        SkillExecutionProbe probe,
        string probeJson,
        CancellationToken cancellationToken)
    {
        var recipe = BuildRecipe(draft, verdict);

        // From the insert until the engine has confirmed the recipe, every failure has to take the row
        // back out. The row is live on every instance the moment it exists, and nothing else in the
        // system would ever remove it again: the next run reopens the CLUSTER, not the orphaned recipe.
        // A cancellation is the case that matters most here - a deployment mid-refresh would otherwise
        // leave an unverified, potentially hijacking recipe enabled forever - so the withdrawal runs on
        // CancellationToken.None. Cancelling the cleanup with the same token that caused it is how the
        // orphan would be created rather than avoided.
        try
        {
            await _recipeRepository.AddAsync(recipe, cancellationToken);
            await WriteTriggerPhrasesAsync(recipe.Name, verdict.Trigger!, cancellationToken);
            await _catalogRefresher.RefreshAsync(ActivationReason, cancellationToken);

            // The one question that needs the row to exist: the engine walks every enabled recipe in
            // sort order, so only it can say whether this wish now reaches this recipe rather than an
            // older one.
            var plan = await _recipeEngine.ResolveAsync(
                cluster.IntentExcerpt, cluster.Locale, ProbeRights, cancellationToken);

            if (plan != null && string.Equals(plan.Name, recipe.Name, StringComparison.OrdinalIgnoreCase))
            {
                return await ConfirmAsync(cluster, draft, candidateId, probe, probeJson, recipe, cancellationToken);
            }

            var reason = plan == null
                ? "The wish still starts no capability at all."
                : $"The wish starts '{plan.Name}' instead.";

            await WithdrawAsync(recipe, CancellationToken.None);
            await FailAsync(
                candidateId, SkillLearningCandidateStatuses.RoutingFailed, probeJson, reason, CancellationToken.None);

            return CapabilityLearningOutcome.Failure(reason);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception, "Activation of capability '{Recipe}' failed; withdrawing it again", recipe.Name);

            await WithdrawAsync(recipe, CancellationToken.None);
            await FailAsync(
                candidateId,
                SkillLearningCandidateStatuses.Generated,
                probeJson,
                exception.Message,
                CancellationToken.None);

            // An activation that broke down says nothing about whether the composition is any good, so
            // it must not spend the cluster's attempt budget - the same rule the execution oracle
            // follows for an identity it could not mint.
            return CapabilityLearningOutcome.Unjudged(exception.Message);
        }
    }

    private async Task<CapabilityLearningOutcome> ConfirmAsync(
        SkillLearningClusterContext cluster,
        LearnedRecipeDraft draft,
        Guid candidateId,
        SkillExecutionProbe probe,
        string probeJson,
        AgentRecipe recipe,
        CancellationToken cancellationToken)
    {
        await _candidateRepository.UpdateVerdictAsync(
            candidateId,
            SkillLearningCandidateStatuses.Active,
            routingResultJson: null,
            probeJson,
            errorText: null,
            DateTime.UtcNow,
            cancellationToken);

        await FreezeGoldenCaseAsync(cluster, recipe.Name, cancellationToken);

        _logger.LogInformation(
            "Learned capability '{Recipe}' with {Steps} step(s), first use owed: {NeedsFirstUse}",
            recipe.Name, draft.Steps.Count, !probe.FullyExecuted);

        return CapabilityLearningOutcome.Success(recipe.Name, !probe.FullyExecuted);
    }

    private static AgentRecipe BuildRecipe(LearnedRecipeDraft draft, RecipeDraftVerdict verdict) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = verdict.Name!,
            Goal = draft.Goal,
            GoalTranslations = new Dictionary<string, string>(draft.GoalTranslations, StringComparer.Ordinal),
            TriggerJson = JsonSerializer.Serialize(verdict.Trigger, RecipeJsonOptions),
            StepsJson = JsonSerializer.Serialize(draft.Steps, RecipeJsonOptions),
            IsEnabled = true,
            SortOrder = SkillLearningDefaults.LearnedRecipeSortOrder,
            Origin = AgentRecipeOrigins.Learned
        };

    // Ordered by how much each step matters. Removing the row is the one thing that must happen: the
    // engine reads the table on every call, so a soft-deleted recipe stops firing immediately, refresh
    // or no refresh. The phrases and the index rebuild are tidying, and neither may be allowed to throw
    // the withdrawal away - this method is itself the failure path, and a failure inside it would leave
    // exactly the live orphan it exists to prevent.
    private async Task WithdrawAsync(AgentRecipe recipe, CancellationToken cancellationToken)
    {
        await _recipeRepository.DeleteAsync(recipe.Id, cancellationToken);

        try
        {
            await _phraseRepository.ReplaceForLanguageAsync(
                SkillPhraseOwnerKinds.Recipe,
                recipe.Name,
                SkillPhraseKinds.Keyword,
                SkillPhraseSources.Learned,
                SkillPhraseLanguages.Undetermined,
                [],
                cancellationToken: cancellationToken);

            await _catalogRefresher.RefreshAsync(RollbackReason, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Capability '{Recipe}' was withdrawn but its phrases or the index could not be cleaned up",
                recipe.Name);
        }
    }

    // Same write the seed loader performs for a hand-written recipe: the knowledge index builds a
    // recipe's embedding text from these phrases, so without them the semantic fallback would only ever
    // see the goal.
    private async Task WriteTriggerPhrasesAsync(
        string recipeName, RecipeTrigger trigger, CancellationToken cancellationToken) =>
        await _phraseRepository.ReplaceForLanguageAsync(
            SkillPhraseOwnerKinds.Recipe,
            recipeName,
            SkillPhraseKinds.Keyword,
            SkillPhraseSources.Learned,
            SkillPhraseLanguages.Undetermined,
            RecipeTriggerWordExtractor.Extract(trigger),
            cancellationToken: cancellationToken);

    private async Task FailAsync(
        Guid candidateId,
        string status,
        string? executionResultJson,
        string? error,
        CancellationToken cancellationToken)
    {
        await _candidateRepository.UpdateVerdictAsync(
            candidateId, status, routingResultJson: null, executionResultJson, error, null, cancellationToken);

        _logger.LogInformation("Rejected capability variant: {Reason}", error);
    }

    private async Task FreezeGoldenCaseAsync(
        SkillLearningClusterContext cluster, string recipeName, CancellationToken cancellationToken)
    {
        if (await _goldenCaseRepository.ExistsAsync(cluster.IntentExcerpt, recipeName, cancellationToken))
        {
            return;
        }

        await _goldenCaseRepository.AddAsync(
            new SkillLearningGoldenCase
            {
                Id = Guid.NewGuid(),
                Query = cluster.IntentExcerpt,
                Locale = cluster.Locale,
                ExpectedSourceId = recipeName,
                ClusterId = cluster.ClusterId
            },
            cancellationToken);
    }

    // Only what retrieval already offered for this wish, and only what may be composed at all. Offering
    // the whole catalogue would neither fit a prompt nor produce a better answer: a skill retrieval did
    // not surface for the utterance is not a skill this wish is about.
    private IReadOnlyList<CapabilityBuildingBlock> BuildBlocks(IReadOnlyList<string> candidateSkills)
    {
        var blocks = new List<CapabilityBuildingBlock>();

        foreach (var name in candidateSkills.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var descriptor = _registry.GetSkillByName(name);
            if (descriptor == null
                || string.Equals(descriptor.ExecutionType, LlmExecutionTypes.UiAction, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var riskClass = _riskClassifier.Classify(descriptor);
            if (riskClass != SkillRiskClass.ReadOnly && riskClass != SkillRiskClass.Reversible)
            {
                continue;
            }

            blocks.Add(new CapabilityBuildingBlock(
                descriptor.Name,
                descriptor.Description,
                [.. descriptor.Parameters.Select(Describe)],
                riskClass == SkillRiskClass.ReadOnly));
        }

        return blocks;
    }

    private static string Describe(SkillParameter parameter) =>
        parameter.Required ? $"{parameter.Name} ({parameter.Type}, required)" : $"{parameter.Name} ({parameter.Type})";

    private static IReadOnlyList<string> BuildExamples(IReadOnlyList<AgentRecipe> recipes) =>
    [
        .. recipes
            .Where(recipe => string.Equals(recipe.Origin, AgentRecipeOrigins.Seed, StringComparison.Ordinal))
            .Take(ExampleRecipeCount)
            .Select(recipe => JsonSerializer.Serialize(
                new
                {
                    name = recipe.Name,
                    goal = recipe.Goal,
                    goalTranslations = recipe.GoalTranslations,
                    trigger = recipe.TriggerJson,
                    steps = recipe.StepsJson
                },
                RecipeJsonOptions))
    ];

    // Read straight from the cases rather than carried on the cluster context: that context is what the
    // generator sees, and no user id may ever reach a language model.
    private async Task<string?> ResolveOwnerAsync(Guid clusterId, CancellationToken cancellationToken)
    {
        var cases = await _caseRepository.ListByClusterAsync(
            clusterId, SkillLearningDefaults.ClusterCaseSampleSize, cancellationToken);

        return cases.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.UserId))?.UserId;
    }

    private static string Serialize(LearnedRecipeDraft draft) =>
        JsonSerializer.Serialize(
            new
            {
                name = draft.Name,
                goal = draft.Goal,
                goalTranslations = draft.GoalTranslations,
                trigger = draft.Trigger,
                steps = draft.Steps
            },
            RecipeJsonOptions);
}
