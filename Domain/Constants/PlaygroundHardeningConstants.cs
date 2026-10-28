// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Pinned models, limits, error codes and messages of the Playground hardening (Deployment:IsPlayground only).
/// The model defaults also stand in for every missing or empty configuration value, so a misconfiguration can
/// never mean "the visitor picks the model" or "an expensive fallback model".
/// </summary>
public static class PlaygroundHardeningConstants
{
    public const string ModelsSectionName = "Playground:Models";

    /// <summary>
    /// Chat model (llm_models.model_id) every Playground chat turn runs on, whatever the client sends.
    /// </summary>
    public const string DefaultChatModelId = "deepseek-flash";

    /// <summary>
    /// Provider (llm_providers.provider_id) that serves the transcription cleanup on the Playground.
    /// </summary>
    public const string DefaultTranscriptionProviderId = "groq";

    /// <summary>
    /// Provider-side model id of the transcription cleanup. Measured against the Groq API: it answers without
    /// reasoning tokens, so a short correction costs a fraction of what the gpt-oss reasoning models burn.
    /// </summary>
    public const string DefaultTranscriptionApiModelId = "qwen/qwen3.8-27b";

    public const int PlaygroundMaxPromptChars = 1000;

    /// <summary>
    /// Longest page-context text field (route, ids, dates, entity type) the Playground passes on. The route is rendered
    /// unabridged into the system prompt, so an unbounded value would inflate every turn; real routes and ids are far shorter.
    /// </summary>
    public const int PlaygroundMaxContextFieldChars = 256;

    /// <summary>
    /// Most selected client ids of the page context the Playground passes on; a larger multi-selection is cut off.
    /// </summary>
    public const int PlaygroundMaxSelectedClientIds = 50;

    public const string PromptTooLongErrorCode = "PLAYGROUND_PROMPT_TOO_LONG";

    public static readonly string PromptTooLongMessage =
        $"The message is longer than the Playground allows ({PlaygroundMaxPromptChars} characters).";

    public const string FeatureBlockedErrorCode = "PLAYGROUND_FEATURE_BLOCKED";

    public const string FeatureBlockedMessage = "This function is not available on the public Playground.";

    public const string ErrorCodeExtension = "errorCode";

    public const string ChatModelUnavailableMessageFormat =
        "The Playground chat model '{0}' is not available or not enabled. No other model is used as a fallback.";

    public const int ConversationTtlMinutes = 120;

    public const int MaxStoredConversations = 500;

    public const string AnonymousVisitorKey = "anonymous";

    public const string ConversationKeySeparator = "|";
}
