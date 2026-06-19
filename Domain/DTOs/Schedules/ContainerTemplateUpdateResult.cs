// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.DTOs.Schedules;

public class ContainerTemplateUpdateResult
{
    public List<Guid> CreatedItems { get; set; } = new();
    public List<Guid> UpdatedItems { get; set; } = new();
    public List<ContainerTemplateItem> DeletedItems { get; set; } = new();
}
