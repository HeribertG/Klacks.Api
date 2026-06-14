// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Slim qualification projection for the schedule row header bubble.
/// </summary>
/// <remarks>
/// Carries the master qualification's emoji and localized name (for the hover tooltip)
/// plus the employee's level and optional expiry date.
/// </remarks>
public class ScheduleQualificationResource
{
    public Guid QualificationId { get; set; }

    public string? Emoji { get; set; }

    public MultiLanguage Name { get; set; } = new();

    public QualificationLevel Level { get; set; }

    public DateOnly? ValidUntil { get; set; }
}
