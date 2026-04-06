// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response DTO containing all children (sub-works, sub-breaks, and work changes) of a container work.
/// </summary>
namespace Klacks.Api.Application.DTOs.Schedules;

public class ContainerWorkChildrenResource
{
    public List<WorkResource> SubWorks { get; set; } = new();

    public List<BreakResource> SubBreaks { get; set; } = new();

    public List<WorkChangeResource> SubWorkChanges { get; set; } = new();

    public string? ParentStartBase { get; set; }

    public string? ParentEndBase { get; set; }

    public int? ParentTransportMode { get; set; }
}
