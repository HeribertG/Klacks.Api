// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The single place that decides whether a request parameter may be sent to a given model. Providers
/// used to carry their own allow-lists, which meant every new model generation had to be remembered in
/// several files at once. Precedence is: an operator declaration on the model wins, otherwise the
/// built-in default rule for the provider's contract family applies, otherwise the caller's fallback.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public static class LLMModelParameterPolicy
{
    // Anthropic removed the sampling parameters from Claude Sonnet 5 / Opus 4.7 onwards; sending
    // "temperature" to those models fails the whole request with HTTP 400. Allow-list of the older
    // families that still accept it, matched by prefix so dated variants such as
    // claude-haiku-4-5-20251001 are covered. An unknown model falls through to "omit", because every
    // model line since 4.7 has dropped the parameter.
    private static readonly string[] AnthropicTemperatureCapablePrefixes =
    [
        "claude-opus-4-6",
        "claude-opus-4-5",
        "claude-opus-4-1",
        "claude-opus-4-0",
        "claude-sonnet-4-6",
        "claude-sonnet-4-5",
        "claude-sonnet-4-0",
        "claude-haiku-4-5",
        "claude-3"
    ];

    // OpenAI's reasoning-oriented models reject a caller-supplied temperature. Omitting the field is
    // always accepted (the API then applies its own default of 1), so this is an allow-list of the
    // families that DO honour a custom temperature; every other model, including unknown or future
    // ones, has it omitted, which is the fail-safe default.
    private static readonly string[] OpenAiTemperatureCapablePrefixes =
    [
        "gpt-3.5",
        "gpt-4",
        "gpt-5.2",
        "gpt-5.3",
        "gpt-5.4"
    ];

    private const string OpenAiChatModelMarker = "chat";

    /// <summary>
    /// Decides whether <paramref name="parameterName"/> may be included in the outgoing request.
    /// </summary>
    /// <param name="family">Contract family the calling provider belongs to</param>
    /// <param name="apiModelId">Provider-side model identifier the request targets</param>
    /// <param name="parameterName">Wire name of the parameter, see <see cref="LLMModelParameterNames"/></param>
    /// <param name="declared">Operator declarations for this model; null or empty means undecided</param>
    /// <param name="fallback">Result for parameters that have no built-in rule for this family</param>
    public static bool IsSupported(
        LLMParameterDefaultFamily family,
        string apiModelId,
        string parameterName,
        ModelParameterSupport? declared,
        bool fallback = true)
    {
        if (declared is not null && declared.TryGetDeclaration(parameterName, out var declaredSupport))
        {
            return declaredSupport;
        }

        if (parameterName != LLMModelParameterNames.Temperature)
        {
            return fallback;
        }

        return family switch
        {
            LLMParameterDefaultFamily.Anthropic => HasCapablePrefix(apiModelId, AnthropicTemperatureCapablePrefixes),
            LLMParameterDefaultFamily.OpenAiCompatible =>
                apiModelId.Contains(OpenAiChatModelMarker, StringComparison.OrdinalIgnoreCase) ||
                HasCapablePrefix(apiModelId, OpenAiTemperatureCapablePrefixes),
            _ => fallback
        };
    }

    private static bool HasCapablePrefix(string apiModelId, string[] prefixes) =>
        prefixes.Any(prefix => apiModelId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
}
