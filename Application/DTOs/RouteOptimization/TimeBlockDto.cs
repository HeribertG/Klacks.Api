// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Input DTO for a TimeBlock from the frontend.
/// </summary>
/// <param name="Id">Block-ID (z.B. Absence-ID)</param>
/// <param name="Name">Display name</param>
/// <param name="FixedStartTime">Fester Start (HH:mm) — nur bei Unmovable</param>
/// <param name="FixedEndTime">Festes Ende (HH:mm) — nur bei Unmovable</param>
/// <param name="DurationMinutes">Dauer in Minuten</param>
/// <param name="IsMovable">True = verschiebbar, False = fixe Position</param>

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class TimeBlockDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? FixedStartTime { get; set; }
    public string? FixedEndTime { get; set; }
    public double DurationMinutes { get; set; }
    public bool IsMovable { get; set; }
}
