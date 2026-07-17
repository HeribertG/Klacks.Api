// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shadow-mode implementation of the recipe-vs-skill margin signal. When a recipe fires by keyword
/// trigger, it embeds the RAW user message in the same mE5 space as skill retrieval, force-includes the
/// recipe's served skills alongside the KNN neighbours, cross-encoder scores them all, and logs the
/// margin (best served minus best foreign) for later threshold calibration. It changes nothing about the
/// live routing decision — it only observes and logs. Guarded by an opt-in config flag so it costs zero
/// extra compute when disabled (off in production unless enabled, on in Development).
/// </summary>
/// <param name="embeddingProvider">Embeds the raw message in the SAME mE5 space as skill retrieval.</param>
/// <param name="rerankerProvider">Cross-encoder that scores the raw message against candidate skills.</param>
/// <param name="repository">KNN neighbour lookup plus by-key fetch for force-included served skills.</param>
/// <param name="configuration">Holds the opt-in shadow-mode flag (off unless explicitly enabled).</param>
/// <param name="logger">Structured, grep-able shadow log (prefix [recipe-skill-margin]).</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public sealed class RecipeSkillMarginEvaluator : IRecipeSkillMarginEvaluator
{
    public const string ShadowModeEnabledConfigKey = "Assistant:RecipeSkillMarginShadowMode:Enabled";

    // Placeholder gate threshold for the shadow log ONLY (no behavioural branch): the real thresholds
    // will be fixed from the shadow-log + eval-set distributions. epsilon = 0.0 means "would gate when a
    // foreign skill out-scores every served skill", i.e. margin below zero.
    private const double GatePlaceholderEpsilon = 0.0;

    private const string LogPrefix = "[recipe-skill-margin]";

    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IRerankerProvider _rerankerProvider;
    private readonly IKnowledgeIndexRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RecipeSkillMarginEvaluator> _logger;

    public RecipeSkillMarginEvaluator(
        IEmbeddingProvider embeddingProvider,
        IRerankerProvider rerankerProvider,
        IKnowledgeIndexRepository repository,
        IConfiguration configuration,
        ILogger<RecipeSkillMarginEvaluator> logger)
    {
        _embeddingProvider = embeddingProvider;
        _rerankerProvider = rerankerProvider;
        _repository = repository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<RecipeSkillMarginResult?> EvaluateAndLogAsync(
        RecipeSkillMarginRequest request,
        CancellationToken cancellationToken)
    {
        if (!_configuration.GetValue(ShadowModeEnabledConfigKey, false))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.Message) || request.ServedSkillNames.Count == 0)
        {
            return null;
        }

        var servedSet = new HashSet<string>(request.ServedSkillNames, StringComparer.OrdinalIgnoreCase);
        var (permissions, adminBypass, permissionScope) = ResolvePermissionScope(request.UserRights);

        var queryVec = await _embeddingProvider.EmbedQueryAsync(request.Message, cancellationToken);
        var neighbours = await _repository.FindNearestAsync(
            queryVec, permissions, adminBypass, KnowledgeIndexConstants.MaxRerankerCandidates, cancellationToken);

        // bestServed / bestForeign live in the SKILL score space only — an Endpoint or Recipe neighbour
        // must never become the foreign competitor and skew the margin.
        var skillNeighbours = neighbours
            .Where(n => n.Kind == KnowledgeEntryKind.Skill)
            .ToList();

        var presentServed = new HashSet<string>(
            skillNeighbours.Where(n => servedSet.Contains(n.SourceId)).Select(n => n.SourceId),
            StringComparer.OrdinalIgnoreCase);

        // Force-include: served skills the KNN top-N did not surface still need a comparable score so the
        // margin never falsely reads "served skill absent" merely because it fell outside the window.
        var missingServed = request.ServedSkillNames
            .Where(name => !presentServed.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var forcedEntries = missingServed.Count == 0
            ? (IReadOnlyList<KnowledgeEntry>)Array.Empty<KnowledgeEntry>()
            : await _repository.GetByKeysAsync(
                missingServed.Select(name => (KnowledgeEntryKind.Skill, name)).ToList(), cancellationToken);

        var candidates = new List<KnowledgeEntry>(skillNeighbours);
        var seen = new HashSet<string>(skillNeighbours.Select(n => n.SourceId), StringComparer.OrdinalIgnoreCase);
        foreach (var entry in forcedEntries)
        {
            if (entry.Kind == KnowledgeEntryKind.Skill && seen.Add(entry.SourceId))
            {
                candidates.Add(entry);
            }
        }

        if (candidates.Count == 0)
        {
            _logger.LogInformation(
                "{Prefix} recipe='{Recipe}' produced no skill candidates for the raw message; margin not computable. permissionScope={Scope}",
                LogPrefix, request.RecipeName, permissionScope);
            return null;
        }

        var texts = candidates.Select(c => c.Text).ToList();

        // No 0.05 tool-budget cutoff is applied here: that cutoff bounds the tool list sent to the LLM,
        // whereas this comparison needs a score for every candidate to measure a fair margin.
        var scores = await _rerankerProvider.ScoreAsync(request.Message, texts, cancellationToken);

        (string Skill, double Score)? bestServed = null;
        (string Skill, double Score)? bestForeign = null;
        var scoredServed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < candidates.Count && i < scores.Length; i++)
        {
            var sourceId = candidates[i].SourceId;
            var score = scores[i];
            if (servedSet.Contains(sourceId))
            {
                scoredServed.Add(sourceId);
                if (bestServed is null || score > bestServed.Value.Score)
                {
                    bestServed = (sourceId, score);
                }
            }
            else if (bestForeign is null || score > bestForeign.Value.Score)
            {
                bestForeign = (sourceId, score);
            }
        }

        var servedNotScored = servedSet.Count - scoredServed.Count;

        double? margin = bestServed is not null && bestForeign is not null
            ? bestServed.Value.Score - bestForeign.Value.Score
            : null;

        var wouldGate = margin is not null && margin.Value < GatePlaceholderEpsilon;

        var result = new RecipeSkillMarginResult(
            bestServed?.Skill,
            bestServed?.Score,
            bestForeign?.Skill,
            bestForeign?.Score,
            margin,
            wouldGate,
            permissionScope,
            servedNotScored,
            request.OldDetectorDecision);

        _logger.LogInformation(
            "{Prefix} recipe='{Recipe}' trigger='{Trigger}' served=[{Served}] bestServed={BestServed}({BestServedScore}) " +
            "bestForeign={BestForeign}({BestForeignScore}) margin={Margin} wouldGateAtEpsilon={WouldGate} epsilon={Epsilon} " +
            "oldDetectorDecision={OldDecision} oldDetectorCompeting=[{OldCompeting}] permissionScope={Scope} servedSkillsNotScored={NotScored}",
            LogPrefix,
            request.RecipeName,
            request.MatchedTrigger,
            string.Join(",", request.ServedSkillNames),
            result.BestServedSkill ?? "none",
            FormatScore(result.BestServedScore),
            result.BestForeignSkill ?? "none",
            FormatScore(result.BestForeignScore),
            FormatScore(result.Margin),
            result.WouldGateAtPlaceholderThreshold,
            GatePlaceholderEpsilon.ToString("F4", CultureInfo.InvariantCulture),
            result.OldDetectorDecision,
            string.Join(",", request.OldDetectorCompetingSkills),
            result.PermissionScope,
            result.ServedSkillsNotScored);

        return result;
    }

    private static (IReadOnlyCollection<string> Permissions, bool AdminBypass, string Scope) ResolvePermissionScope(
        IReadOnlyCollection<string>? userRights)
    {
        if (userRights is null)
        {
            // Defensive fallback: the current callers (LLMService, SkillToolsetAssembler) always pass a
            // non-null userRights, so this branch is not reached on the live path. Should a future caller be
            // unable to resolve permissions, do NOT pretend an empty set / isAdmin:false is the real
            // permission set — surface the limitation via the scope label and open the KNN with an admin
            // bypass so the shadow signal still sees every potential competitor (documented, not silent).
            return (Array.Empty<string>(), true, "user-rights-unavailable-admin-bypass");
        }

        var isAdmin = userRights.Contains(Roles.Admin);
        return (userRights, isAdmin, isAdmin ? "user-rights-admin" : "user-rights-scoped");
    }

    private static string FormatScore(double? value)
        => value.HasValue ? value.Value.ToString("F4", CultureInfo.InvariantCulture) : "n/a";
}
