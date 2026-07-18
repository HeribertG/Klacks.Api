// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Setup;

[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public class RegionSetupProfile
{
    /// <summary>
    /// Highest schema version this binary understands; a profile file must declare exactly this value.
    /// </summary>
    public const int CurrentVersion = 1;

    /// <summary>
    /// Schema version of this profile file; must equal <see cref="CurrentVersion"/>.
    /// Required so future breaking schema changes fail fast instead of silently misapplying settings.
    /// </summary>
    [JsonRequired]
    public int Version { get; set; }

    public string? Region { get; set; }

    public RegionSetupLanguages? Languages { get; set; }

    public RegionSetupLocale? Locale { get; set; }

    public RegionSetupCalendar? Calendar { get; set; }

    public RegionSetupWorktime? Worktime { get; set; }

    public RegionSetupSurcharges? Surcharges { get; set; }

    public RegionSetupExport? Export { get; set; }

    public RegionSetupCompliance? Compliance { get; set; }

    /// <summary>
    /// Named industry preset blocks (K20), keyed by industry slug (e.g. "spitex", "security").
    /// Entity-import payloads (SchedulingRule presets, qualification catalogs) — reconciled on every
    /// startup via per-row import keys, never gated by a section marker. All blocks are imported; they
    /// are selectable presets and do not change behavior until a contract references them.
    /// </summary>
    public Dictionary<string, RegionSetupIndustryProfile>? IndustryProfiles { get; set; }

    /// <summary>
    /// Industry slugs of this profile that are "armed" for the installation: only these industries are
    /// offered in selection lists, while ALL industryProfiles blocks are still imported. Every entry
    /// must be a key of the IndustryProfiles map of the same file; an empty list is rejected — omit the
    /// field to keep all industries active. Applied exactly once via its own section marker.
    /// </summary>
    public List<string>? ActiveIndustries { get; set; }

    /// <summary>
    /// Marketplace package identity (country + version) of this profile. Written to settings on every
    /// run — never marker-gated — so a newer downloaded package version updates the recorded identity.
    /// </summary>
    public RegionSetupPackage? Package { get; set; }

    /// <summary>
    /// Calculation macros shipped by the profile (K20 entity import): compiled at import time,
    /// reconciled per row on every startup, customer-edited rows never overwritten. A non-custom
    /// function demotes the current seeded/unedited holder; a customer-created holder fails the import.
    /// </summary>
    public List<RegionSetupMacro>? Macros { get; set; }

    /// <summary>
    /// Demo/training data (5000 fake clients, shifts, contracts) is seeded only when true;
    /// omitted or false means no demo data is seeded.
    /// </summary>
    public bool? SeedDemoData { get; set; }
}
