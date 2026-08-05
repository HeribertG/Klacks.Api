// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Constants;

/// <summary>
/// Central constants for the knowledge index module (model names, cache paths, cutoffs, table names).
/// </summary>
public static class KnowledgeIndexConstants
{
    public const string TableName = "knowledge_index";

    // Must match the active embedding model and the vector(n) column in knowledge_index. Changing it
    // needs a migration; every stored vector becomes invalid, which the synchronizer handles by
    // itself because EmbeddingSpaceId is folded into the stored text hash.
    public const int EmbeddingDimension = 768;

    // Cap on how many texts are run through a single ONNX inference call. Embedding/reranking the
    // full set in one batch builds an activation tensor proportional to (batch x sequence x hidden)
    // that spikes resident memory past the container limit (OOM-killed during startup index build,
    // worst case on an empty index that must embed every skill). Chunking bounds the per-run peak
    // and lets memory be reclaimed between chunks.
    public const int EmbeddingBatchSize = 16;

    // Lowered 16 -> 4 on 2026-08-03. Since OnnxRerankerProvider groups candidates by token length, the
    // batch size decides how tight those groups are: a smaller batch means less padding to the longest
    // row in the group. Measured on a 10-core host over a realistic candidate set (25 pairs, token
    // lengths min 29 / median 144 / max 557), length-sorted with the throughput session options:
    //   batch 16 -> 2560 ms    batch 8 -> 1919 ms    batch 4 -> 1755 ms    batch 2 -> 3994 ms
    // Below 4 the per-run overhead and the lost intra-op parallelism outweigh the padding saved, so 2 is
    // markedly worse than 4 — this is a minimum, not a "smaller is better" knob. For reference, the same
    // set took 23 400 ms in the configuration shipped before that day (single thread, arrival order).
    public const int RerankBatchSize = 4;

    // Raised 12 -> 20 on 2026-08-01. Vector-stage recall over the 625-case, 25-language set:
    // @12 = 573, @16 = 581, @20 = 594, @25 = 601. The ceiling is the tool budget, not the measurement:
    // MaxToolsForProviderCeiling (30) minus 9 alwaysOn skills leaves 21 slots for retrieved ones, so
    // 20 is the largest value that still fits. Note the figures above are vector-stage; while the
    // cross-encoder still orders the pool, Take(DefaultTopK) runs after it, so part of the gain is
    // only collectable once the reranker is out of the ordering path.
    public const int DefaultTopK = 20;

    // Floor on the raw cross-encoder score, applied before Take(DefaultTopK). Measured against the hard
    // golden set (KnowledgeIndexHardGoldenSetDiHostTests, 104 + 69 confusable cases): the reranker's
    // score distribution is bimodal — median 0.92, but a whole cluster of correct targets sits below
    // 0.05 while still being ranked 2nd or 3rd. At 0.05 those were discarded despite correct ordering.
    // Toolset recall@12 per floor (core / extended / avg entries kept for a question with no valid
    // tool), re-measured 2026-07-29 after the trigger-keyword rebuild — the previous figures in this
    // comment predate it and read ~2 points high on the core set:
    //   0.05   -> 84.6% / 56.5% / 0.7      0.0005 -> 93.3% / 76.8% / 7.5
    //   0.02   -> 87.5% / 66.7% / 1.7      0.0002 -> 93.3% / 78.3% / 8.9
    //   0.01   -> 89.4% / 69.6% / 2.3      0.0001 -> 93.3% / 81.2% / 10.3
    //   0.005  -> 90.4% / 73.9% / 3.2      0.0     -> 93.3% / 81.2% / 12.0
    //   0.001  -> 93.3% / 75.4% / 6.4
    //   0.0001 -> 93.3% / 81.2% / 10.3  <- current
    // Below 0.001 the core set is flat: its two remaining misses sit outside DefaultTopK as well
    // (rank 18 and 25), so no floor can reach them — every further gain is extended-set only, and it
    // is paid for in off-topic entries. 0.0 is strictly worse than 0.0001: identical recall, 12.0
    // entries instead of 10.3, so removing the floor is never the right move.
    // 0.0001 chosen 2026-07-30 (Heribert): buys the full extended-set gain of +5.8 points, the whole
    // of which the reranker stage was discarding, for 3.9 more entries on an off-topic question.
    // Do not raise this without re-measuring: the cost of a high floor is invisible in production,
    // since a discarded target looks like a capability the assistant never had.
    public const double DefaultScoreCutoff = 0.0001;

    // How many KNN candidates go into the cross-encoder reranking pass. Must stay >= DefaultTopK,
    // otherwise the reranker can never surface enough candidates to fill topK.
    public const int MaxRerankerCandidates = 25;

    // How many pg_trgm lexical candidates are fetched per query, mirroring MaxRerankerCandidates so
    // the fused (semantic + lexical) candidate set feeding the reranker never grows past the existing
    // reranker budget — hybrid retrieval changes which candidates reach the reranker, not how many.
    public const int LexicalCandidateCount = MaxRerankerCandidates;

    // Reciprocal Rank Fusion constant (Cormack, Clarke & Buettcher 2009: "Reciprocal Rank Fusion
    // Outperforms Condorcet and Individual Rank Learning Methods"). Score per list = 1 / (k + rank).
    // k=60 is the value the paper found robust across collections without per-dataset tuning; it damps
    // the influence of rank 1 in a single list so one list cannot dominate fusion outright.
    public const int ReciprocalRankFusionK = 60;

    // Cap on the tool list sent to the LLM provider per turn at the reference (>=100k effective input
    // limit) tier. Must stay >= (enabled alwaysOn skills + DefaultTopK) so retrieved (non-alwaysOn)
    // skills are never fully squeezed out by alwaysOn ones during truncation. Enforced by
    // SkillToolBudgetGuardTests. Shared by both the streaming and non-streaming chat paths (previously
    // two diverging private consts: 22 vs 30). Mirrors ContextBudgetPolicy.MaxToolsForProviderCeiling,
    // the actual per-turn cap now varies with the model's effective input limit (P2 of the Klacksy
    // memory redesign) — this constant is the reference-tier ceiling, never exceeded even for larger
    // context windows.
    public const int MaxToolsForProvider = Klacks.Api.Domain.Services.Assistant.ContextBudgetPolicy.MaxToolsForProviderCeiling;

    // multilingual-e5-base since 2026-07-30, was -small (384 dimensions). Three arrangement
    // experiments on the small model came up empty — per-phrase vectors scored by their best match
    // cost 7 cases, rank fusion gained 3, and stripping the foreign-language phrases from the index
    // text moved the same cases around for a net zero. All three rearrange material without adding
    // discriminative power, and the neighbourhood dump shows why: for a failing query the top six
    // candidates sit within four thousandths of each other, so the ordering inside that band is
    // barely more than noise. 384 dimensions do not separate 350 closely related tools across five
    // languages; -base doubles the room per token (MIRACL nDCG@10 59.3 -> 62.5).
    // -large would be the next step up but ships in ONNX external-data format (a 545 KB graph plus a
    // 2.2 GB weights file), which the single-file ModelLoader cannot fetch as it stands.
    public const string EmbeddingModelName = "multilingual-e5-base";
    public const string EmbeddingModelFileName = "model.onnx";
    public const string EmbeddingTokenizerFileName = "tokenizer.json";
    public const string EmbeddingModelSha256 = "84a4d426f7e87a6bf5bf195f0bae2c4a7d15f675b23ca96f42fab8326d7a77aa";
    public const string EmbeddingTokenizerSha256 = "62c24cdc13d4c9952d63718d6c9fa4c287974249e16b7ade6d5a85e7bbb75626";
    public const string EmbeddingModelUrl =
        "https://huggingface.co/intfloat/multilingual-e5-base/resolve/main/onnx/model.onnx";
    public const string EmbeddingTokenizerUrl =
        "https://huggingface.co/intfloat/multilingual-e5-base/resolve/main/tokenizer.json";

    public const string RerankerModelName = "mmarco-mMiniLMv2-L12-H384-v1";
    public const string RerankerModelFileName = "model.onnx";
    public const string RerankerTokenizerFileName = "tokenizer.json";
    public const string RerankerModelSha256 = "3e9a03ed1e966f7c5288dd4230e3d6a9bf5e3a170a06f1f4241c5bca12c6487c";
    public const string RerankerTokenizerSha256 = "62c24cdc13d4c9952d63718d6c9fa4c287974249e16b7ade6d5a85e7bbb75626";
    public const string RerankerModelUrl =
        "https://huggingface.co/cross-encoder/mmarco-mMiniLMv2-L12-H384-v1/resolve/main/onnx/model.onnx";
    public const string RerankerTokenizerUrl =
        "https://huggingface.co/cross-encoder/mmarco-mMiniLMv2-L12-H384-v1/resolve/main/tokenizer.json";

    public const string ModelsCacheSubdirectory = "Cache/Models";

    public const string ModelsRootConfigKey = "KnowledgeIndex:ModelsRoot";

    // Optional override for ONNX-backed embedding/reranking. When unset, the platform is probed and
    // ONNX is disabled on Windows ARM64 (Snapdragon X), where the runtime's bundled cpuinfo cannot
    // detect the SoC and crashes the process. Set to "true"/"false" to force the behaviour.
    public const string OnnxEnabledConfigKey = "KnowledgeIndex:OnnxEnabled";

    // Builds both inference sessions right after startup rather than inside the first chat request.
    // Defaults to true. The opt-out exists for memory-capped hosts: the sessions are resident for the
    // lifetime of the process, and warming them moves that allocation earlier rather than avoiding it.
    // Production runs in a 2.5 GB container, so if a host ever OOMs during startup, this is the switch.
    public const string WarmupEnabledConfigKey = "KnowledgeIndex:WarmupEnabled";

    // Prefix of the EmbeddingSpaceId produced by the local ONNX provider. Anything else means the
    // process fell back to a remote embedding API, which changes retrieval quality — see the startup
    // warning in KnowledgeIndexStartupService.
    public const string LocalEmbeddingSpacePrefix = "onnx:";

    public const string HttpClientName = "knowledge-index-models";
}
