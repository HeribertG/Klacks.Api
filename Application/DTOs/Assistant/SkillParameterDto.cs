// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Assistant;

public record SkillParameterDto
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required SkillParameterType Type { get; init; }
    public required bool Required { get; init; }
    public object? DefaultValue { get; init; }
    public IReadOnlyList<string>? EnumValues { get; init; }
}
