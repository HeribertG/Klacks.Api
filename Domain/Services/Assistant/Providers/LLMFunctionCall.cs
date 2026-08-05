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
    public string? UiActionSteps { get; set; }
    public LLMFunctionResultKind ResultKind { get; set; }
    public List<string> DataJson { get; set; } = new();
}