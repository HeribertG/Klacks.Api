// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Assistant.Skills;

public class SkillBridgeResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string ResultType { get; set; } = "Data";
    public string? UiActionSteps { get; set; }
    public Dictionary<string, object>? UiActionParameters { get; set; }
}
