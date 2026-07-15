// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Normalized (lowercase) values of the SURCHARGE_STACKING_MODE setting. The setting no longer drives
/// the surcharge arithmetic itself — stacking is a structural property of the macro assigned to a shift
/// (MacroFunctionEnum.Standard vs. StandardAdditive) — it only selects which of the two seeded standard
/// macros DefaultShiftMacroResolver auto-assigns to newly created shifts.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SurchargeStackingModeValues
{
    public const string HighestWins = "highestwins";
    public const string Additive = "additive";
}
