// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response DTO containing all children (sub-works and sub-breaks) of a container work.
/// </summary>
namespace Klacks.Api.Application.DTOs.Schedules;

public class ContainerWorkChildrenResource
{
    public List<WorkResource> SubWorks { get; set; } = new();

    public List<BreakResource> SubBreaks { get; set; } = new();
}
