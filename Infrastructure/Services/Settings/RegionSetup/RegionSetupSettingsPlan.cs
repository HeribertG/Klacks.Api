// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Outcome of planning the marker-gated settings sections of a region-setup profile, computed before
/// any write.
/// </summary>
/// <param name="LanguagesToInstall">Language plugin codes to install</param>
/// <param name="Settings">Planned (type, value) setting writes; callers may append further entries</param>
/// <param name="SectionMarkersToWrite">Section marker keys to record as applied</param>

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed record RegionSetupSettingsPlan(
    List<string> LanguagesToInstall,
    List<(string Type, string Value)> Settings,
    List<string> SectionMarkersToWrite);
