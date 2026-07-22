// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IReadOnlyToolsetFilter
{
    IReadOnlyList<SkillDescriptor> Filter(
        IReadOnlyList<SkillDescriptor> candidates,
        string? excludeSkillName = null);
}
