// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public class LLMProviderRequest
{
    public string Message { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public List<LLMMessage> ConversationHistory { get; set; } = new();
    public List<LLMFunction> AvailableFunctions { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 2000;
    public decimal CostPerInputToken { get; set; }
    public decimal CostPerOutputToken { get; set; }
    public bool Stream { get; set; }

    /// <summary>
    /// Per-turn tool-call policy passed through to providers that use the modern OpenAI "tools"
    /// format ("auto" lets the model decide, "required" forces at least one tool call). Null falls
    /// back to the provider default ("auto"). Set to "required" for state-changing (mutation) turns
    /// so a weak model cannot answer in prose while silently performing no action.
    /// </summary>
    public string? ToolChoice { get; set; }

    /// <summary>
    /// Optional PNG image bytes that providers with vision support attach as a
    /// content block alongside the user <see cref="Message"/>. Providers that
    /// cannot handle images ignore this field. Currently consumed by Holistic Harmonizer
    /// to render the schedule grid for vision-capable Claude models.
    /// </summary>
    public byte[]? ImagePng { get; set; }
}