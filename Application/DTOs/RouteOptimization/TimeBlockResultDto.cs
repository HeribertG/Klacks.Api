// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Output DTO for a placed TimeBlock sent to the frontend.
/// </summary>
/// <param name="Id">Block ID</param>
/// <param name="Name">Display name</param>
/// <param name="StartTime">Calculated start time (HH:mm:ss)</param>
/// <param name="EndTime">Calculated end time (HH:mm:ss)</param>
/// <param name="InsertionPosition">Position in the route (after which shift)</param>
/// <param name="IsMovable">Whether the block was movable</param>

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class TimeBlockResultDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int InsertionPosition { get; set; }
    public bool IsMovable { get; set; }
}
