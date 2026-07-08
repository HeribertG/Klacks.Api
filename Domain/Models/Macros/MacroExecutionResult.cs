// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Macros;

/// <summary>
/// Result of a macro execution with optional surcharges/work time value and a typed surcharge breakdown.
/// </summary>
/// <param name="Success">Indicates whether the macro execution was successful</param>
/// <param name="ResultValue">The extracted decimal value from the DefaultResult, or null if none exists</param>
/// <param name="Surcharges">Typed surcharge portions whose sum equals the unrounded total; empty when the macro emits no typed surcharge messages</param>
public record MacroExecutionResult(
    bool Success,
    decimal? ResultValue,
    IReadOnlyList<MacroSurchargeItem> Surcharges)
{
    public MacroExecutionResult(bool success, decimal? resultValue)
        : this(success, resultValue, Array.Empty<MacroSurchargeItem>())
    {
    }
}
