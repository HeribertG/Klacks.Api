// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Hard-coded fallbacks for scheduling-policy values when neither the global settings table nor
/// the client's contract carries a meaningful number. These are NOT business rules — the real
/// value chain is settings → contract → scheduling-rule. Defaults exist only so an empty database
/// does not produce zero-value caps that would bypass every check.
/// </summary>
public static class SchedulingPolicyDefaults
{
    public const double MinRestHours = 11.0;
    public const double MaxDailyHours = 10.0;
    public const int MaxConsecutiveDays = 6;
}
