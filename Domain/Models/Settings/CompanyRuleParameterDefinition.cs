// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One parameter the company-rule intake can collect for a given rule kind: its name, an English
/// description shown to the admin, the expected data type, whether it is required, and — where the type
/// is Enum or a numeric range — the allowed values or inclusive Min/Max bounds used for validation.
/// </summary>

using System.Collections.Generic;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Settings;

public sealed class CompanyRuleParameterDefinition
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public CompanyRuleParameterDataType DataType { get; init; }

    public bool Required { get; init; }

    public IReadOnlyList<string>? EnumValues { get; init; }

    public decimal? Min { get; init; }

    public decimal? Max { get; init; }
}
