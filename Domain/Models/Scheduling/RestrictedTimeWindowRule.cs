// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A seasonal daily forbidden-time-window rule (K16): work is not permitted between
/// <see cref="DailyStart"/> and <see cref="DailyEnd"/> on any calendar day that falls inside the season
/// spanning (<see cref="SeasonFromMonth"/>, <see cref="SeasonFromDay"/>) through
/// (<see cref="SeasonToMonth"/>, <see cref="SeasonToDay"/>) - e.g. the UAE/Saudi midday outdoor-work ban
/// 12:30-15:00 from 15 June to 15 September. The season is stored as month/day integer tuples (never the
/// "MM-DD" string) so the year-boundary wrap (e.g. 15 Nov .. 15 Feb) is a pure tuple comparison, identical
/// in the API evaluator and the optimizer filter. Scope: <see cref="AppliesToGroupTag"/> matches
/// case-insensitively against a Group.Name; a shift is in scope when a non-deleted GroupItem links it to a
/// group of that name inside the link's validity window. An empty tag means the rule applies to ALL shifts.
/// Industry-axis scoping (SchedulingRuleId, as on the K10/K18 sibling rules) is deliberately NOT modelled
/// here: the group tag IS K16's scope axis, and a dormant column would have neither a writer nor a reader.
/// Rows are written exclusively by the region-setup entity-import mechanism (K20);
/// ImportSourceKey/ImportContentHash (see <see cref="Klacks.Api.Domain.Common.IImportableEntity"/>)
/// drive that re-apply logic.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Scheduling;

public class RestrictedTimeWindowRule : BaseEntity, IImportableEntity
{
    public int SeasonFromMonth { get; set; }

    public int SeasonFromDay { get; set; }

    public int SeasonToMonth { get; set; }

    public int SeasonToDay { get; set; }

    public TimeOnly DailyStart { get; set; }

    public TimeOnly DailyEnd { get; set; }

    public string AppliesToGroupTag { get; set; } = string.Empty;

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
