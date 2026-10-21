// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Per-section marker model of the region setup: reads which section markers exist, decides per section
/// whether to skip, backfill (legacy whole-file marker present) or apply it, and builds the fail-fast
/// settings plan of the applied sections. Each section has its own marker so a section is applied
/// exactly once, independently of the others.
/// </summary>
/// <param name="settingsRepository">Reads the section marker settings</param>
/// <param name="languagePlanner">Validates the languages section</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RegionSetupSectionPlanner
{
    private static readonly IReadOnlyDictionary<RegionSetupSection, string> SectionMarkerKeys = new Dictionary<RegionSetupSection, string>
    {
        [RegionSetupSection.Languages] = SettingKeys.RegionSetupAppliedLanguages,
        [RegionSetupSection.Locale] = SettingKeys.RegionSetupAppliedLocale,
        [RegionSetupSection.Calendar] = SettingKeys.RegionSetupAppliedCalendar,
        [RegionSetupSection.Worktime] = SettingKeys.RegionSetupAppliedWorktime,
        [RegionSetupSection.Surcharges] = SettingKeys.RegionSetupAppliedSurcharges,
        [RegionSetupSection.Export] = SettingKeys.RegionSetupAppliedExport,
        [RegionSetupSection.Compliance] = SettingKeys.RegionSetupAppliedCompliance,
        [RegionSetupSection.Industries] = SettingKeys.RegionSetupAppliedIndustries,
    };

    // Frozen at the sections that existed before per-section markers were introduced. A section already
    // covered by the legacy whole-file marker counts as applied without rewriting its settings. This list
    // must NEVER grow: a newly introduced section (e.g. "compliance", added below) must go through the
    // normal Apply path on every installation, even one where the legacy whole-file marker is present.
    // Deliberately hard-coded rather than derived from SectionMarkerKeys.Keys, which does grow.
    private static readonly IReadOnlySet<RegionSetupSection> LegacySections = new HashSet<RegionSetupSection>
    {
        RegionSetupSection.Languages,
        RegionSetupSection.Locale,
        RegionSetupSection.Calendar,
        RegionSetupSection.Worktime,
        RegionSetupSection.Surcharges,
        RegionSetupSection.Export,
    };

    private readonly ISettingsRepository _settingsRepository;
    private readonly RegionSetupLanguagePlanner _languagePlanner;

    public RegionSetupSectionPlanner(ISettingsRepository settingsRepository, RegionSetupLanguagePlanner languagePlanner)
    {
        _settingsRepository = settingsRepository;
        _languagePlanner = languagePlanner;
    }

    public async Task<Dictionary<RegionSetupSection, bool>> LoadSectionMarkerExistenceAsync()
    {
        var result = new Dictionary<RegionSetupSection, bool>();
        foreach (var (section, markerKey) in SectionMarkerKeys)
        {
            result[section] = await _settingsRepository.GetSetting(markerKey) != null;
        }

        return result;
    }

    public static Dictionary<RegionSetupSection, RegionSetupSectionAction> DetermineSectionActions(
        RegionSetupProfile profile,
        IReadOnlyDictionary<RegionSetupSection, bool> sectionMarkerExists,
        bool globalMarkerPresent)
    {
        var actions = new Dictionary<RegionSetupSection, RegionSetupSectionAction>();
        foreach (var section in SectionMarkerKeys.Keys)
        {
            if (sectionMarkerExists[section])
            {
                actions[section] = RegionSetupSectionAction.Skip;
            }
            else if (globalMarkerPresent && LegacySections.Contains(section))
            {
                actions[section] = RegionSetupSectionAction.Backfill;
            }
            else if (IsSectionPresent(section, profile))
            {
                actions[section] = RegionSetupSectionAction.Apply;
            }
            else
            {
                actions[section] = RegionSetupSectionAction.Skip;
            }
        }

        return actions;
    }

    public RegionSetupSettingsPlan BuildPlan(
        RegionSetupProfile profile,
        IReadOnlyDictionary<RegionSetupSection, RegionSetupSectionAction> actions)
    {
        var settings = new List<(string Type, string Value)>();
        var sectionMarkersToWrite = new List<string>();
        var languagesToInstall = new List<string>();

        foreach (var (section, action) in actions)
        {
            if (action == RegionSetupSectionAction.Skip)
            {
                continue;
            }

            sectionMarkersToWrite.Add(SectionMarkerKeys[section]);
            if (action == RegionSetupSectionAction.Backfill)
            {
                continue;
            }

            switch (section)
            {
                case RegionSetupSection.Languages:
                    languagesToInstall = _languagePlanner.ValidateLanguages(profile.Languages);
                    _languagePlanner.AddDefaultLanguageSetting(profile.Languages, languagesToInstall, settings);
                    break;
                case RegionSetupSection.Locale:
                    RegionSetupSettingsBuilder.AddLocaleSettings(profile.Locale, settings);
                    break;
                case RegionSetupSection.Calendar:
                    RegionSetupSettingsBuilder.AddCalendarSettings(profile.Calendar, settings);
                    break;
                case RegionSetupSection.Worktime:
                    RegionSetupSettingsBuilder.AddWorktimeSettings(profile.Worktime, settings);
                    break;
                case RegionSetupSection.Surcharges:
                    RegionSetupSurchargeSettingsBuilder.AddSurchargeSettings(profile.Surcharges, settings);
                    break;
                case RegionSetupSection.Export:
                    RegionSetupSettingsBuilder.AddExportSettings(profile.Export, settings);
                    break;
                case RegionSetupSection.Compliance:
                    RegionSetupComplianceSettingsBuilder.AddComplianceSettings(profile.Compliance, settings);
                    break;
                case RegionSetupSection.Industries:
                    RegionSetupSettingsBuilder.AddActiveIndustriesSettings(profile, settings);
                    break;
            }
        }

        return new RegionSetupSettingsPlan(languagesToInstall, settings, sectionMarkersToWrite);
    }

    private static bool IsSectionPresent(RegionSetupSection section, RegionSetupProfile profile) => section switch
    {
        RegionSetupSection.Languages => profile.Languages != null,
        RegionSetupSection.Locale => profile.Locale != null,
        RegionSetupSection.Calendar => profile.Calendar != null,
        RegionSetupSection.Worktime => profile.Worktime != null,
        RegionSetupSection.Surcharges => profile.Surcharges != null,
        RegionSetupSection.Export => profile.Export != null,
        RegionSetupSection.Compliance => profile.Compliance != null,
        RegionSetupSection.Industries => profile.ActiveIndustries != null,
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown region setup section."),
    };
}
