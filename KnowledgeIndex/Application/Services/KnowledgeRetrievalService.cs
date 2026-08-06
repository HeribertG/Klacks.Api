// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
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

    // Enough to tell queries apart when counting repeats in a log, short enough to stay readable.
    private const int ShortHashBytes = 4;

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
    private readonly RetrievalCallCounter? _callCounter;

    public KnowledgeRetrievalService(
        IEmbeddingProvider embeddingProvider,
        IRerankerProvider rerankerProvider,
        IKnowledgeIndexRepository repository,
        ILogger<KnowledgeRetrievalService>? logger = null,
        RetrievalCallCounter? callCounter = null)
    {
        _embeddingProvider = embeddingProvider;
        _rerankerProvider = rerankerProvider;
        _repository = repository;
        _logger = logger ?? NullLogger<KnowledgeRetrievalService>.Instance;
        _callCounter = callCounter;
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

        var call = _callCounter?.NextCall() ?? 0;
        var embedWatch = Stopwatch.StartNew();
        var queryVec = await _embeddingProvider.EmbedQueryAsync(userQuery, cancellationToken);
        var embedMs = embedWatch.ElapsedMilliseconds;

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
        var knnWatch = Stopwatch.StartNew();
        var candidates = await _repository.FindNearestAsync(
            queryVec, userPermissions, isAdmin, KnowledgeIndexConstants.MaxRerankerCandidates, cancellationToken,
            kindFilter);
        var knnMs = knnWatch.ElapsedMilliseconds;

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

        var rerankWatch = Stopwatch.StartNew();
        var scores = await _rerankerProvider.ScoreAsync(userQuery, texts, cancellationToken);
        var rerankMs = rerankWatch.ElapsedMilliseconds;

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
        // kindFilter already ran inside the KNN query, so a recipe caller gets a candidate pool of
        // recipes instead of the best-of-everything (457 skills against 24 recipes - a kind-blind
        // top-25 is almost always all skills, scored expensively and then thrown away). The re-check here is
        // a guard only: it keeps the contract independent of the repository honouring the parameter,
        // and it still has to run before Take so a mixed pool could never displace matching entries.
        var ranked = filtered
            .Zip(scores, (e, s) => (Entry: e, RawScore: s))
            .Where(p => p.RawScore >= KnowledgeIndexConstants.DefaultScoreCutoff)
            .Where(p => kindFilter is null || p.Entry.Kind == kindFilter.Value)
            .Select(p => new RetrievalCandidate(p.Entry, ApplyRouteBoost(p.Entry, p.RawScore, routeBoostedSkills)))
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();

        LogPass(call, userQuery, userPermissions, kindFilter, embedMs, knnMs, rerankMs, texts, topK, ranked.Count);

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

    // One grep-able line per pass. Until this existed, not a single stopwatch sat anywhere in the
    // retrieval chain, and the LLM-Stage timings do not help: AssembleAsync runs outside
    // LLMService.PrepareContextAsync, where that measurement lives. Neither the duration of a pass nor
    // how many passes a turn triggered was readable from any log.
    //
    // "call" is the pass ordinal inside the turn — a turn can trigger more than one, and since the
    // cross-encoder dominates the cost, two passes cost roughly twice as much as one.
    //
    // "chars" is the total candidate text handed to the cross-encoder. It is the closest proxy for its
    // real workload that is available here without a tokenizer, and it is the number the language
    // filter is meant to move: the encoder scores query+candidate pairs, so its cost tracks the text,
    // not the candidate count.
    //
    // "query"/"perms" are hashes, never the text: the raw query is already logged at Debug elsewhere,
    // and this line is meant to be safe at Information. The pair also answers whether a cross-request
    // cache would ever hit — count repeated (query, perms) pairs in the logs before building one.
    // NOTE: production log level is Warning, so this stays invisible there until it is raised.
    private void LogPass(
        int call,
        string userQuery,
        IReadOnlyCollection<string> userPermissions,
        KnowledgeEntryKind? kindFilter,
        long embedMs,
        long knnMs,
        long rerankMs,
        List<string> texts,
        int topK,
        int returned)
    {
        if (!_logger.IsEnabled(LogLevel.Information))
        {
            return;
        }

        _logger.LogInformation(
            "[retrieval] call={Call} query={QueryHash} perms={PermHash} kind={Kind} " +
            "embed={EmbedMs}ms knn={KnnMs}ms rerank={RerankMs}ms cands={Candidates} chars={Chars} topK={TopK} returned={Returned}",
            call,
            ShortHash(userQuery),
            ShortHash(string.Join(",", userPermissions.OrderBy(p => p, StringComparer.Ordinal))),
            kindFilter?.ToString() ?? "any",
            embedMs,
            knnMs,
            rerankMs,
            texts.Count,
            texts.Sum(t => t.Length),
            topK,
            returned);
    }

    private static string ShortHash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash, 0, ShortHashBytes).ToLowerInvariant();
    }
}
