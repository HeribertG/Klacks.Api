// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant.Providers;

public class LLMFunctionCall
{
    public string FunctionName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string? Result { get; set; }
    public bool Success { get; set; } = true;
    public bool RequiresConfirmation { get; set; }

    /// <summary>
    /// True when the loop rejected this call as a repeat of a write skill that already ran earlier
    /// in the same turn. Like RequiresConfirmation this sets Success=false without being a real
    /// skill failure, so failure-driven consumers (reflection, grounding skip, last-error notice)
    /// must exclude it.
    /// </summary>
    public bool IsRejectedRepeat { get; set; }
    public string? UiActionSteps { get; set; }
    public Guid? UiActionTrackingId { get; set; }
    public LLMFunctionResultKind ResultKind { get; set; }
    public List<string> DataJson { get; set; } = new();
}