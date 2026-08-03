// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Logging;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Retrieves relevant skills and endpoints from the knowledge index using KNN vector search,
/// skill/endpoint deduplication, cross-encoder reranking, and score cutoff filtering.
/// </summary>
/// <param name="embeddingProvider">Converts the user query into a vector embedding.</param>
/// <param name="rerankerProvider">Cross-encoder that scores query-candidate pairs.</param>
/// <param name="repository">Repository for pgvector KNN search with permission filtering.</param>
/// <param name="logger">Logger for retrieval diagnostics on the hot chat path.</param>
public sealed class KnowledgeRetrievalService : IKnowledgeRetrievalService
{
    // Soft signal only: nudges skills relevant to the user's current page slightly higher in the
    // ranking, but never filters — a page-unrelated request must still be answerable. Deliberately a
    // small, hand-curated table (mirrors SuggestionsRanker.RouteHints) rather than a per-skill "pages"
    // tag on all ~256 seeded skills, since the boost magnitude can't be calibrated on hosts where
    // retrieval is inert (see KnowledgeIndex ARM64 dead-retrieval note) and a wrong tag would silently
    // degrade unrelated queries.
    // Multiplicative, not additive, because the cross-encoder's score distribution is bimodal (see
    // KnowledgeIndexConstants.DefaultScoreCutoff): a fixed summand inverts a pair whenever the absolute
    // gap is smaller than the summand, so the former +0.05 spanned the entire lower mode and flattened
    // every candidate below 0.05 into a near-tie once DefaultScoreCutoff dropped to 0.001. A factor
    // inverts a pair iff stronger/weaker < RouteBoostFactor — the same relative strength at every scale.
    // The exact value inside that property is NOT measured: no golden-set run has ever passed a non-null
    // currentRoute, so there is no data on route-boosted ranking at all. Two tests bound the admissible
    // window: RetrieveAsync_CurrentRouteMatchesBoostTable_LiftsMatchingSkillAboveHigherRawScore requires
    // > 1.06, RetrieveAsync_RouteBoost_DoesNotLiftAMuchWeakerSkillOverAStrongerOne requires < 6.95.
    // 1.5 sits inside that window; it is not a calibrated optimum.
    private const double RouteBoostFactor = 1.5;

    private static readonly (string RouteFragment, string[] SkillNames)[] RouteSkillBoosts =
    {
        ("/workplace/schedule", new[] { "open_schedule", "read_schedule_state", "create_shift", "cut_shift", "search_shifts" }),
        ("/workplace/clients", new[] { "search_employees", "get_client_details", "update_client", "add_client_to_group" }),
        ("/workplace/absence", new[] { "create_absence", "list_absence_types", "cover_absence" }),
        ("/workplace/settings/llm", new[] { "list_llm_providers", "create_llm_provider", "update_llm_provider" }),
        ("/workplace/settings", new[] { "get_general_settings", "update_general_settings" }),
    };

    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IRerankerProvider _rerankerProvider;
    private readonly IKnowledgeIndexRepository _repository;
    private readonly ILogger<KnowledgeRetrievalService> _logger;

    public KnowledgeRetrievalService(
        IEmbeddingProvider embeddingProvider,
        IRerankerProvider rerankerProvider,
        IKnowledgeIndexRepository repository,
        ILogger<KnowledgeRetrievalService>? logger = null)
    {
        _embeddingProvider = embeddingProvider;
        _rerankerProvider = rerankerProvider;
        _repository = repository;
        _logger = logger ?? NullLogger<KnowledgeRetrievalService>.Instance;
    }

    public async Task<RetrievalResult> RetrieveAsync(
        string userQuery,
        IReadOnlyCollection<string> userPermissions,
        bool isAdmin,
        int topK,
        string? currentRoute,
        CancellationToken cancellationToken,
        KnowledgeEntryKind? kindFilter = null)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            return new RetrievalResult([]);

        var queryVec = await _embeddingProvider.EmbedQueryAsync(userQuery, cancellationToken);

        // Retrieval is semantic-only. Lexical RRF fusion (FindLexicalAsync + KnowledgeIndexRankFuser)
        // is fully implemented and kept deliberately unwired: measured against the hard golden set
        // (KnowledgeIndexHardGoldenSetDiHostTests, 104 deliberately confusable cases) it produced a
        // byte-identical result to semantic-only — same top-1 (56), same top-3 (77), same pre-rerank
        // recall@25 (100), the exact same 48 failing queries, only 4th-decimal score jitter. The lexical
        // channel was verified alive (pg_trgm word_similarity returns real, non-empty candidates), so
        // that is a genuine null result, not a dead-channel artifact. Wiring it in costs one extra
        // database round-trip per turn for no measured benefit.
        // Before re-enabling, re-measure — and note the calls must stay SEQUENTIAL: the repository holds
        // a single scoped NpgsqlConnection, which does not support concurrent commands.
        var candidates = await _repository.FindNearestAsync(
            queryVec, userPermissions, isAdmin, KnowledgeIndexConstants.MaxRerankerCandidates, cancellationToken);

        if (candidates.Count == 0)
            return new RetrievalResult([]);

        var wrappedEndpoints = candidates
            .Where(c => c.Kind == KnowledgeEntryKind.Skill && c.ExposedEndpointKey is not null)
            .Select(c => c.ExposedEndpointKey!)
            .ToHashSet();

        var filtered = candidates
            .Where(c => c.Kind == KnowledgeEntryKind.Skill || !wrappedEndpoints.Contains(c.SourceId))
            .ToList();

        if (filtered.Count == 0)
            return new RetrievalResult([]);

        var texts = filtered.Select(f => f.Text).ToList();
        var scores = await _rerankerProvider.ScoreAsync(userQuery, texts, cancellationToken);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Knowledge retrieval scores for query {Query}: {Scores}",
                userQuery.ForLog(),
                string.Join(", ", filtered.Zip(scores).Select(p => $"{p.First.SourceId}:{p.Second:F3}")));
        }

        var routeBoostedSkills = ResolveRouteBoostedSkills(currentRoute);

        // Two separate guards. The cutoff runs on the raw reranker score and filters before the boost is
        // applied, so an irrelevant skill (raw score below cutoff) can never enter the result just
        // because the user happens to be on its page. That the reordering among the survivors stays
        // proportional is the job of RouteBoostFactor being multiplicative, not of this ordering.
        //
        // kindFilter has to run BEFORE Take, not after it in the caller. The index holds 454 skills
        // against 24 recipes, so the highest scoring candidates are almost always skills: a caller
        // asking for the best 3 recipes by taking the best 3 of everything and filtering afterwards
        // was left with nothing in the ordinary case, having already paid for scoring all 25 pairs.
        // Filtering here costs nothing extra - every candidate is scored either way, because Take
        // never bounded the reranker's work.
        var ranked = filtered
            .Zip(scores, (e, s) => (Entry: e, RawScore: s))
            .Where(p => p.RawScore >= KnowledgeIndexConstants.DefaultScoreCutoff)
            .Where(p => kindFilter is null || p.Entry.Kind == kindFilter.Value)
            .Select(p => new RetrievalCandidate(p.Entry, ApplyRouteBoost(p.Entry, p.RawScore, routeBoostedSkills)))
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();

        return new RetrievalResult(ranked);
    }

    private static HashSet<string>? ResolveRouteBoostedSkills(string? currentRoute)
    {
        if (string.IsNullOrWhiteSpace(currentRoute))
        {
            return null;
        }

        HashSet<string>? boosted = null;
        foreach (var (fragment, skillNames) in RouteSkillBoosts)
        {
            if (currentRoute.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                boosted ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                boosted.UnionWith(skillNames);
            }
        }

        return boosted;
    }

    private static double ApplyRouteBoost(KnowledgeEntry entry, double score, HashSet<string>? routeBoostedSkills)
    {
        if (routeBoostedSkills != null &&
            entry.Kind == KnowledgeEntryKind.Skill &&
            routeBoostedSkills.Contains(entry.SourceId))
        {
            return score * RouteBoostFactor;
        }

        return score;
    }
}
