// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Application.Constants;

/// <summary>
/// Central constants for the knowledge index module (model names, cache paths, cutoffs, table names).
/// </summary>
public static class KnowledgeIndexConstants
{
    public const string TableName = "knowledge_index";

    public const int EmbeddingDimension = 384;
    public const int KnnTopN = 30;
    public const int DefaultTopK = 5;
    public const double DefaultScoreCutoff = 0.3;

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
    public const string RerankerModelSha256 = "";
    public const string RerankerTokenizerSha256 = "";
    public const string RerankerModelUrl =
        "https://huggingface.co/cross-encoder/mmarco-mMiniLMv2-L12-H384-v1/resolve/main/onnx/model.onnx";
    public const string RerankerTokenizerUrl =
        "https://huggingface.co/cross-encoder/mmarco-mMiniLMv2-L12-H384-v1/resolve/main/tokenizer.json";

    public const string ModelsCacheSubdirectory = "Klacks/models";
}
