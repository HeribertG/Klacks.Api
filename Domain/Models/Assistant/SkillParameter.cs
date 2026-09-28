// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public record SkillParameter(
    string Name,
    string Description,
    SkillParameterType Type,
    bool Required,
    object? DefaultValue = null,
    IReadOnlyList<string>? EnumValues = null,
    string? JsonSchema = null
);
