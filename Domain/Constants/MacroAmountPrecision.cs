// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Number of decimal places to which macro amounts (result values adjusted by surcharge rate modes or by overtime
/// stacking) are rounded, shared by the production work path and the macro dry-run.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class MacroAmountPrecision
{
    public const int DecimalPlaces = 2;
}
