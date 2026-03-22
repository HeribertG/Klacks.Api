// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration for enabling/disabling individual background services.
/// Allows targeted control per API instance during horizontal scaling.
/// </summary>
/// <param name="ScheduleTimeline">Enables the ScheduleTimeline service</param>
/// <param name="PeriodHours">Enables the PeriodHours service</param>
/// <param name="MemoryCleanup">Enables the MemoryCleanup service</param>
/// <param name="Heartbeat">Enables the Heartbeat service</param>
/// <param name="Embedding">Enables the Embedding service</param>
/// <param name="SkillGapSuggestion">Enables the SkillGapSuggestion service</param>
/// <param name="EmailPolling">Enables the EmailPolling service</param>
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
