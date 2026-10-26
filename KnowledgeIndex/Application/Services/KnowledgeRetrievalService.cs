// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    internal static readonly IReadOnlyList<(string RouteFragment, string[] SkillNames)> RouteSkillBoosts =
    new (string RouteFragment, string[] SkillNames)[]
    {
        ("/workplace/schedule", new[] { "open_schedule", "read_schedule_state", "create_shift", "cut_shift", "search_shifts" }),
        ("/workplace/client", new[] { "search_employees", "get_client_details", "update_client", "add_client_to_group" }),
        ("/workplace/absence", new[] { "create_absence", "list_absence_types", "cover_absence" }),
        ("/workplace/settings", new[]
        {
            "get_general_settings", "update_general_settings",
            "list_llm_providers", "create_llm_provider", "update_llm_provider"
        }),
    };

    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IRerankerProvider _rerankerProvider;
    private readonly IKnowledgeIndexRepository _repository;
    private readonly ILogger<KnowledgeRetrievalService> _logger;
    private readonly RetrievalCallCounter? _callCounter;
    private readonly SkillRerankOptions _skillRerank;
    private readonly ISkillRerankThreshold _threshold;

    /// <param name="skillRerank">Whether skill passes are reranked; null keeps the shipped default (Always).</param>
    /// <param name="threshold">The live Adaptive threshold shared with the tuner; null starts one from skillRerank.</param>
    public KnowledgeRetrievalService(
        IEmbeddingProvider embeddingProvider,
        IRerankerProvider rerankerProvider,
        IKnowledgeIndexRepository repository,
        ILogger<KnowledgeRetrievalService>? logger = null,
        RetrievalCallCounter? callCounter = null,
        SkillRerankOptions? skillRerank = null,
        ISkillRerankThreshold? threshold = null)
    {
        _embeddingProvider = embeddingProvider;
        _rerankerProvider = rerankerProvider;
        _repository = repository;
        _logger = logger ?? NullLogger<KnowledgeRetrievalService>.Instance;
        _callCounter = callCounter;
        _skillRerank = skillRerank ?? SkillRerankOptions.Default;
        _threshold = threshold ?? new SkillRerankThresholdState(_skillRerank);
    }

    public async Task<RetrievalResult> RetrieveAsync(
        string userQuery,
        IReadOnlyCollection<string> userPermissions,
        bool isAdmin,
        int topK,
        string? currentRoute,
        CancellationToken cancellationToken,
        KnowledgeEntryKind? kindFilter = null,
        IReadOnlyCollection<string>? sourceIdFilter = null)
    {
        if (string.IsNullOrWhiteSpace(userQuery) || sourceIdFilter is { Count: 0 })
            return new RetrievalResult([]);

        var call = _callCounter?.NextCall() ?? 0;

        // Started before the query is embedded and awaited only where the scores are needed. After an
        // idle unload both sessions are cold, and building the reranker (1.1 s) behind the embedder
        // (2.2-2.9 s) instead of beside it cost the first question after a quiet half hour a full second.
        var rerankerReady = PreloadRerankerAsync(cancellationToken);

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

        var allowedSourceIds = sourceIdFilter == null
            ? null
            : new HashSet<string>(sourceIdFilter, StringComparer.OrdinalIgnoreCase);
        var filtered = candidates
            .Where(c => c.Kind == KnowledgeEntryKind.Skill || !wrappedEndpoints.Contains(c.SourceId))
            .Where(c => allowedSourceIds == null || allowedSourceIds.Contains(c.SourceId))
            .ToList();

        if (filtered.Count == 0)
            return new RetrievalResult([]);

        var texts = filtered.Select(f => f.Text).ToList();

        // Skill passes may keep the vector order (SkillRerankOptions). The cosine margin between the first two
        // candidates is logged on every skill pass, reranked or not, so live traffic keeps calibrating the
        // Adaptive threshold: the starting value is a middle value meant to be sharpened from these lines.
        var decision = kindFilter == KnowledgeEntryKind.Skill
            ? DecideVectorOrder(call, queryVec, filtered)
            : ((double[]? VectorScores, SkillRerankDecision? Decision)?)null;
        var vectorScores = decision?.VectorScores;

        double[] scores;
        long rerankMs;
        if (vectorScores != null)
        {
            scores = vectorScores;
            rerankMs = 0;
        }
        else
        {
            await rerankerReady;

            var rerankWatch = Stopwatch.StartNew();
            scores = await _rerankerProvider.ScoreAsync(userQuery, texts, cancellationToken);
            rerankMs = rerankWatch.ElapsedMilliseconds;
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug(
                "Knowledge retrieval scores for query {QueryHash}: {Scores}",
                ShortHash(userQuery),
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
            // The route boost is a factor tuned for the cross-encoder's spread-out scores. On cosine scores, which
            // sit within a few hundredths of each other, x1.5 would lift every page skill above everything else,
            // so the vector order is kept without it.
            .Select(p => new RetrievalCandidate(
                p.Entry,
                vectorScores != null ? p.RawScore : ApplyRouteBoost(p.Entry, p.RawScore, routeBoostedSkills)))
            .OrderByDescending(c => c.Score)
            .Take(topK)
            .ToList();

        LogPass(call, userQuery, userPermissions, kindFilter, embedMs, knnMs, rerankMs, texts, topK, ranked.Count);

        return new RetrievalResult(ranked) { SkillRerank = decision?.Decision };
    }

    /// <summary>
    /// Returns the cosine scores of the candidates when this skill pass keeps the vector order (null when the
    /// cross-encoder has to run), together with the decision the turn records for the threshold tuner. The margin is
    /// the gap between the first two candidates in KNN order - the same definition as
    /// KnowledgeIndexPhraseFacetDiagnosticsTests.AdaptiveReranking_MarginThresholdSweep, so a threshold read off that
    /// sweep means the same thing here. In Adaptive mode a share of the passes (ExplorationRate) is flipped at random
    /// against the rule: that controlled sample is what lets the tuner compare both orders at the same margin.
    /// Logged on every skill pass, whatever the mode.
    /// </summary>
    /// <param name="call">Pass ordinal for the log line</param>
    /// <param name="queryVector">The embedded query</param>
    /// <param name="candidates">The KNN candidates, nearest first</param>
    private (double[]? VectorScores, SkillRerankDecision Decision) DecideVectorOrder(
        int call, float[] queryVector, IReadOnlyList<KnowledgeEntry> candidates)
    {
        var cosines = CosineScores(queryVector, candidates);
        var margin = cosines.Length > 1 ? cosines[0] - cosines[1] : 1.0;
        var threshold = _threshold.Current;

        var keepVectorOrder = _skillRerank.Mode switch
        {
            SkillRerankMode.Never => true,
            SkillRerankMode.Adaptive => margin >= threshold,
            _ => false
        };

        var explored = _skillRerank.Mode == SkillRerankMode.Adaptive
                       && _threshold.ExplorationRate > 0
                       && Random.Shared.NextDouble() < _threshold.ExplorationRate;
        if (explored)
        {
            keepVectorOrder = !keepVectorOrder;
        }

        _logger.LogInformation(
            "[retrieval] turn={Turn} call={Call} kind=Skill rerankMode={Mode} margin={Margin:F4} threshold={Threshold:F4} reranked={Reranked} explored={Explored}",
            TurnCorrelation.CurrentOrNone, call, _skillRerank.Mode, margin, threshold, !keepVectorOrder, explored);

        return (keepVectorOrder ? cosines : null, new SkillRerankDecision(margin, !keepVectorOrder, explored));
    }

    /// <summary>
    /// Cosine similarity of every candidate to the query, in candidate order. Both sides are normalised here rather
    /// than trusting the stored vectors to be unit length; a candidate without a stored vector scores 0.
    /// </summary>
    private static double[] CosineScores(float[] queryVector, IReadOnlyList<KnowledgeEntry> candidates)
    {
        var queryNorm = Norm(queryVector);
        var scores = new double[candidates.Count];
        for (var i = 0; i < candidates.Count; i++)
        {
            var vector = candidates[i].Embedding;
            var norm = Norm(vector);
            if (queryNorm == 0 || norm == 0 || vector.Length != queryVector.Length)
            {
                continue;
            }

            double dot = 0;
            for (var j = 0; j < vector.Length; j++)
            {
                dot += queryVector[j] * vector[j];
            }

            scores[i] = dot / (queryNorm * norm);
        }

        return scores;
    }

    private static double Norm(float[] vector)
    {
        double sum = 0;
        foreach (var value in vector)
        {
            sum += value * value;
        }

        return Math.Sqrt(sum);
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
    // ChatTurnContextPreparer.PrepareAsync, where that measurement lives. Neither the duration of a pass nor
    // how many passes a turn triggered was readable from any log.
    //
    // "turn" is the ambient chat-turn id, the join key: count the lines carrying the same turn to see
    // how many passes that turn paid for, and match them against the "LLM TTFT ... turn=" line of the
    // same turn. Work outside a chat turn (warmup, background services) logs "-".
    //
    // "call" is the pass ordinal inside the DI scope, NOT inside the turn: the recipe engine resolves
    // in a child scope of its own and therefore starts counting at 1 again. Count "turn" lines, not
    // this ordinal, when asking how many passes a turn triggered.
    //
    // "chars" is the total candidate text handed to the cross-encoder. It is the closest proxy for its
    // real workload that is available here without a tokenizer, and it is the number the language
    // filter is meant to move: the encoder scores query+candidate pairs, so its cost tracks the text,
    // not the candidate count.
    //
    // "query"/"perms" are hashes, never the text: the raw query is already logged at Debug elsewhere,
    // and this line is meant to be safe at Information. The pair also answers whether a cross-request
    // cache would ever hit — count repeated (query, perms) pairs in the logs before building one.
    // Production raises this namespace to Information (appsettings.Production.json), so the line is
    // live there — keep it free of raw query text.
    // Never throws: the preload is only ever a head start. Whatever went wrong here happens again in
    // ScoreAsync a moment later, where it surfaces with the call that actually needs the session - and
    // a request that ends early for lack of candidates must not fail on a task nobody awaited.
    private async Task PreloadRerankerAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _rerankerProvider.EnsureLoadedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Reranker preload failed; the scoring call will report the cause.");
        }
    }

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
            "[retrieval] turn={Turn} call={Call} query={QueryHash} perms={PermHash} kind={Kind} " +
            "embed={EmbedMs}ms knn={KnnMs}ms rerank={RerankMs}ms cands={Candidates} chars={Chars} topK={TopK} returned={Returned}",
            TurnCorrelation.CurrentOrNone,
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
