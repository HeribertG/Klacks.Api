// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Konfiguration zum Aktivieren/Deaktivieren einzelner Background Services.
/// Ermöglicht bei horizontaler Skalierung die gezielte Steuerung pro API-Instanz.
/// </summary>
/// <param name="ScheduleTimeline">Aktiviert den ScheduleTimeline-Service</param>
/// <param name="PeriodHours">Aktiviert den PeriodHours-Service</param>
/// <param name="MemoryCleanup">Aktiviert den MemoryCleanup-Service</param>
/// <param name="Heartbeat">Aktiviert den Heartbeat-Service</param>
/// <param name="Embedding">Aktiviert den Embedding-Service</param>
/// <param name="SkillGapSuggestion">Aktiviert den SkillGapSuggestion-Service</param>
/// <param name="EmailPolling">Aktiviert den EmailPolling-Service</param>
namespace Klacks.Api.Application.Configuration;

public class BackgroundServiceOptions
{
    public const string SectionName = "BackgroundServices";

    public bool ScheduleTimeline { get; set; } = true;
    public bool PeriodHours { get; set; } = true;
    public bool MemoryCleanup { get; set; } = true;
    public bool Heartbeat { get; set; } = true;
    public bool Embedding { get; set; } = true;
    public bool SkillGapSuggestion { get; set; } = true;
    public bool EmailPolling { get; set; } = true;
}
