// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the two short probe requests the Holistic Harmonizer sends before a run: the JSON pre-flight
/// ping and the vision capability check. Both switch thinking off and leave output headroom, because a
/// thinking model shares one output budget between reasoning and answer.
/// </summary>
/// <param name="model">The configured Holistic Harmonizer model; supplies api id, parameters and prices</param>
/// <param name="capabilityPng">The rendered capability token image the model has to read back</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

public static class HolisticHarmonizerProbeRequests
{
    private const double ProbeTemperature = 0.0;

    private const string PingSystemPrompt =
        "You are a JSON-only test endpoint. Reply with exactly: {\"ping\":\"pong\"}\n" +
        "No prose, no markdown, no commentary, no extra fields.";
    private const string PingUserMessage = "Reply with the JSON object as instructed.";

    private const string CapabilitySystemPrompt =
        "You are a deterministic vision-capability verifier for the Klacks Holistic Harmonizer (Wizard 3).\n" +
        "Wizard 3 mutates a bitmap-rendered schedule, so the host accepts only models that genuinely process attached images.\n" +
        "You receive a small PNG containing exactly one short alphabetic token painted in large bold black letters on a yellow box.\n" +
        "Reply with ONE JSON object and nothing else: {\"token\":\"...\"}.\n" +
        "No prose, no markdown, no code fences, no commentary.\n" +
        "If you cannot see or process the image, reply with {\"token\":\"\"}.";
    private const string CapabilityUserMessage =
        "Read the token printed in the attached image and reply with the JSON object only.";

    public static LLMProviderRequest Ping(LLMModel model) =>
        Probe(model, PingUserMessage, PingSystemPrompt, imagePng: null);

    public static LLMProviderRequest Capability(LLMModel model, byte[] capabilityPng) =>
        Probe(model, CapabilityUserMessage, CapabilitySystemPrompt, capabilityPng);

    private static LLMProviderRequest Probe(LLMModel model, string message, string systemPrompt, byte[]? imagePng)
    {
        ArgumentNullException.ThrowIfNull(model);

        return new LLMProviderRequest
        {
            Message = message,
            SystemPrompt = systemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = ProbeTemperature,
            MaxTokens = ModelProbeConstants.ThinkingHeadroomMaxTokens,
            ThinkingBudgetTokens = ThinkingBudgetConstants.Disabled,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken,
            Stream = false,
            ImagePng = imagePng,
        };
    }
}
