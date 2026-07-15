// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Hard-coded fallbacks for surcharge values when neither the scheduling rule, the contract nor the
/// global settings table carries a configured value. These are NOT business rules — the real value
/// chain is scheduling rule → contract → settings. Defaults exist only so an empty database does not
/// leave the night surcharge window undefined.
/// </summary>
public static class SurchargeDefaults
{
    public const string NightStart = "23:00";
    public const string NightEnd = "06:00";
}
