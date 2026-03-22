// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Single validation entry for the schedule error list.
/// Can be Error (collision), Warning (rest period, working time) or Info (understaffing).
/// </summary>
/// <param name="Type">error, warning or info</param>
/// <param name="ClientId">Affected employee</param>
/// <param name="ClientName">Name of the employee</param>
/// <param name="Date">Date of the entry</param>
/// <param name="Comment">Translation key for display</param>
/// <param name="CommentParams">Parameters for the translation</param>
using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Notifications;

public record ScheduleValidationNotificationDto
{
    [JsonConverter(typeof(JsonStringEnumConverter<ScheduleValidationType>))]
    public ScheduleValidationType Type { get; init; }
    public Guid ClientId { get; init; }
    public string ClientName { get; init; } = string.Empty;
    public DateOnly Date { get; init; }
    public string Comment { get; init; } = string.Empty;
    public Dictionary<string, string> CommentParams { get; init; } = new();
}
