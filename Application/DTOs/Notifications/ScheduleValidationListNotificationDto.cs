// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// List of all schedule validation entries for the frontend.
/// Contains collisions, rest period violations, overtime and understaffing.
/// </summary>
/// <param name="Entries">All validation entries</param>
/// <param name="IsFullRefresh">True on initial load, replaces all existing entries</param>
/// <param name="CheckedClientId">Checked client for single check</param>
/// <param name="CheckedDate">Checked date for single check</param>
namespace Klacks.Api.Application.DTOs.Notifications;

public record ScheduleValidationListNotificationDto
{
    public List<ScheduleValidationNotificationDto> Entries { get; init; } = [];
    public bool IsFullRefresh { get; init; }
    public Guid? CheckedClientId { get; init; }
    public DateOnly? CheckedDate { get; init; }
}
