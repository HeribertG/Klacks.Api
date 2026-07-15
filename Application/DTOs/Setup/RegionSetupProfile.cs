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
    /// Demo/training data (5000 fake clients, shifts, contracts) is seeded only when true;
    /// omitted or false means no demo data is seeded.
    /// </summary>
    public bool? SeedDemoData { get; set; }
}
