// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Machine-readable codes of the 409 answers about variants (keys). The exceptions carry them as ConflictCode, the
/// middleware writes them as "errorCode" on the response body, so the Ui can explain each refusal in the user's language.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ScenarioVariantErrorCodes
{
    public const string Consumed = "SCENARIO_VARIANT_CONSUMED";

    public const string InUse = "SCENARIO_VARIANT_IN_USE";

    public const string ShiftCloned = "SCENARIO_VARIANT_SHIFT_CLONED";

    public const string ShiftHasCuts = "SCENARIO_VARIANT_SHIFT_HAS_CUTS";

    public const string ShiftsOutsidePeriod = "SCENARIO_VARIANT_SHIFTS_OUTSIDE_PERIOD";
}
