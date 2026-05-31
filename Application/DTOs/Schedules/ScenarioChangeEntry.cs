// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One effective grid entry that a scenario adds or removes relative to the real plan. Computed by
/// diffing the resolved schedule cells (which already fold in works, replacements and breaks), so it
/// is producer-agnostic: a propose_plan work, a cover_absence replacement and an absence break all
/// surface here.
/// </summary>
/// <param name="EntryType">Resolved entry type (Work, WorkChange, Break, Expenses, ScheduleNote)</param>
/// <param name="ClientId">Employee the entry is recorded against</param>
/// <param name="ReplaceClientId">Replacement employee when the entry is a replacement, otherwise null</param>
/// <param name="Shift">Shift name (stable across the scenario clone), if any</param>
/// <param name="Date">Day of the entry (ISO yyyy-MM-dd)</param>
/// <param name="StartTime">Start time (HH:mm)</param>
/// <param name="EndTime">End time (HH:mm)</param>
/// <param name="IsReplacement">True when this entry is a replacement assignment</param>
public sealed record ScenarioChangeEntry(
    string EntryType,
    Guid ClientId,
    Guid? ReplaceClientId,
    string? Shift,
    string Date,
    string StartTime,
    string EndTime,
    bool IsReplacement);
