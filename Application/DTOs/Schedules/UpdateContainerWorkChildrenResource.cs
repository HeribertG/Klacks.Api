// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request DTO for updating all children of a container work (Savebar pattern).
/// </summary>
namespace Klacks.Api.Application.DTOs.Schedules;

public class UpdateContainerWorkChildrenResource
{
    public List<WorkResource> SubWorks { get; set; } = new();

    public List<BreakResource> SubBreaks { get; set; } = new();

    public List<WorkChangeResource> SubWorkChanges { get; set; } = new();

    public string? ParentStartBase { get; set; }

    public string? ParentEndBase { get; set; }
}
