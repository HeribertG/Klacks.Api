// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Limits shared by every autofill family, next to the family-specific WizardLimits and
/// AutoWizardLimits. The period cap applies to the bitmap engines (Wizard 2 and 3), whose cost grows
/// with agents x days; a quarter still fits.
/// </summary>
public static class AutofillLimits
{
    /// <summary>Maximum days per run for the bitmap engines.</summary>
    public const int MaxPeriodDays = 92;

    /// <summary>Machine code returned in the 400 response body when the period is too long.</summary>
    public const string PeriodTooLongErrorCode = "AUTOFILL_PERIOD_TOO_LONG";

    /// <summary>Machine code returned in the 409 response body when the same run is already going.</summary>
    public const string RunConflictErrorCode = "AUTOFILL_RUN_CONFLICT";

    /// <summary>Wall-clock budget of a benchmark run.</summary>
    public static readonly TimeSpan BenchmarkMaxRuntime = TimeSpan.FromSeconds(120);

    /// <summary>Grace period before a hung benchmark run is cancelled hard.</summary>
    public static readonly TimeSpan BenchmarkHardCancelGrace = TimeSpan.FromSeconds(20);
}
