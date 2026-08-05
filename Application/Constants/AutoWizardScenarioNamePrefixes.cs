// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Name prefixes of the scenarios the AutoWizard chain creates per stage. Localising these is a separate
/// concern; they are collected here so the chain no longer carries them as literals.
/// </summary>
public static class AutoWizardScenarioNamePrefixes
{
    public const string Wizard = "Auto-Erstellung Plan";
    public const string Harmonizer = "Auto-Erstellung Harmonizer";
    public const string HolisticHarmonizer = "Auto-Erstellung";
}
