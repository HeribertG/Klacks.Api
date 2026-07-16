// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Allowed enum values for company-rule intake parameters whose accepted set is not already an existing
/// domain enum. Values use the lowercase canonical form the region-setup importer persists (it
/// normalizes input with Trim().ToLowerInvariant()), so an intake-produced value can be written to the
/// setting unchanged; the validator matches them case-insensitively.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class CompanyRuleParameterValues
{
    public const string RateModeMultiplier = "multiplier";
    public const string RateModeFixedPerHour = "fixedperhour";
    public const string RateModeFixedPerShift = "fixedpershift";

    public const string StackingModeHighestWins = SurchargeStackingModeValues.HighestWins;
    public const string StackingModeAdditive = SurchargeStackingModeValues.Additive;

    public const string OvertimeBasisDay = "day";
    public const string OvertimeBasisWeek = "week";

    public const string EnforcementWarn = "warn";
    public const string EnforcementBlock = "block";
}
