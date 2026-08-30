// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public record SkillResult
{
    public bool Success { get; init; }
    public object? Data { get; init; }
    public string? Message { get; init; }
    public SkillResultType Type { get; init; }
    public Dictionary<string, object>? Metadata { get; init; }
    public string? UiActionSteps { get; init; }
    public Dictionary<string, object>? UiActionParameters { get; init; }

    /// <summary>
    /// Correlation id of a dispatched UiAction (W1.4). Equals the skill_usage_records row id, so the
    /// frontend can report the real outcome of the browser-side execution back via the report endpoint.
    /// </summary>
    public Guid? UiActionTrackingId { get; init; }

    public static SkillResult SuccessResult(object? data, string? message = null)
        => new()
        {
            Success = true,
            Data = data,
            Message = message,
            Type = SkillResultType.Data
        };

    public static SkillResult Error(string message, Dictionary<string, object>? metadata = null)
        => new()
        {
            Success = false,
            Message = message,
            Type = SkillResultType.Error,
            Metadata = metadata
        };

    public static SkillResult Navigation(object? data, string message)
        => new()
        {
            Success = true,
            Data = data,
            Message = message,
            Type = SkillResultType.Navigation
        };

    public static SkillResult Cancelled(string message)
        => new()
        {
            Success = false,
            Message = message,
            Type = SkillResultType.Cancelled
        };

    public static SkillResult UiAction(string steps, Dictionary<string, object> parameters, string message, Guid? uiActionTrackingId = null)
        => new()
        {
            Success = true,
            Message = message,
            Type = SkillResultType.Data,
            UiActionSteps = steps,
            UiActionParameters = parameters,
            UiActionTrackingId = uiActionTrackingId
        };

    public static SkillResult Confirmation(string message, string confirmationToken, object? pendingData = null)
        => new()
        {
            Success = false,
            Message = message,
            Type = SkillResultType.Confirmation,
            Metadata = new Dictionary<string, object>
            {
                ["confirmationToken"] = confirmationToken,
                ["requiresConfirmation"] = true,
                ["pendingData"] = pendingData ?? new object()
            }
        };
}
