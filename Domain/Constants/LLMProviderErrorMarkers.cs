// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response-body markers used to classify an LLM provider HTTP error as an expected, benign
/// model-incompatibility rather than a genuine fault. When a provider replies to a
/// chat/completions probe with one of these messages (e.g. an embedding or audio model tested
/// against the chat endpoint), the error is logged once at warning level without a stack trace.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class LLMProviderErrorMarkers
{
    private const string NotAChatModel = "not a chat model";
    private const string NotSupportedInChatCompletions = "not supported in the v1/chat/completions";

    private static readonly string[] NonChatModelMarkers =
    [
        NotAChatModel,
        NotSupportedInChatCompletions,
    ];

    public static bool IsNonChatModelError(string? responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return false;
        }

        foreach (var marker in NonChatModelMarkers)
        {
            if (responseBody.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
