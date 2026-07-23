// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One parameter the planning-profile setup can collect: its name, an English description of what it
/// means, an English description of how it changes scheduling (so the assistant can explain the
/// consequence in the user's own words), the expected data type, whether it is required, and — where the
/// type is Enum or a numeric range — the allowed values or inclusive Min/Max bounds used for validation.
/// </summary>

using System.Collections.Generic;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Settings;

public sealed class PlanningProfileParameterDefinition
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string PlanningImpact { get; init; } = string.Empty;

    public PlanningProfileParameterDataType DataType { get; init; }

    public bool Required { get; init; }

    public IReadOnlyList<string>? EnumValues { get; init; }

    public decimal? Min { get; init; }

    public decimal? Max { get; init; }
}
