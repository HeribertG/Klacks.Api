// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Constants;

/// <summary>
/// Central constants for the knowledge index module (model names, cache paths, cutoffs, table names).
/// </summary>
public static class KnowledgeIndexConstants
{
    public const string TableName = "knowledge_index";

    public const int EmbeddingDimension = 384;

    // Cap on how many texts are run through a single ONNX inference call. Embedding/reranking the
    // full set in one batch builds an activation tensor proportional to (batch x sequence x hidden)
    // that spikes resident memory past the container limit (OOM-killed during startup index build,
    // worst case on an empty index that must embed every skill). Chunking bounds the per-run peak
    // and lets memory be reclaimed between chunks.
    public const int EmbeddingBatchSize = 16;
    public const int RerankBatchSize = 16;

    public const int KnnTopN = 30;
    public const int DefaultTopK = 5;
    public const double DefaultScoreCutoff = 0.05;

    // Cap on the tool list sent to the LLM provider per turn. Must stay >= (enabled alwaysOn skills +
    // DefaultTopK) so retrieved (non-alwaysOn) skills are never fully squeezed out by alwaysOn ones
    // during truncation. Enforced by SkillToolBudgetGuardTests. Shared by both the streaming and
    // non-streaming chat paths (previously two diverging private consts: 22 vs 30).
    public const int MaxToolsForProvider = 30;

    public const string EmbeddingModelName = "multilingual-e5-small";
    public const string EmbeddingModelFileName = "model.onnx";
    public const string EmbeddingTokenizerFileName = "tokenizer.json";
    public const string EmbeddingModelSha256 = "ca456c06b3a9505ddfd9131408916dd79290368331e7d76bb621f1cba6bc8665";
    public const string EmbeddingTokenizerSha256 = "0b44a9d7b51c3c62626640cda0e2c2f70fdacdc25bbbd68038369d14ebdf4c39";
    public const string EmbeddingModelUrl =
        "https://huggingface.co/intfloat/multilingual-e5-small/resolve/main/onnx/model.onnx";
    public const string EmbeddingTokenizerUrl =
        "https://huggingface.co/intfloat/multilingual-e5-small/resolve/main/tokenizer.json";

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

    public const string HttpClientName = "knowledge-index-models";
}
