// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A statutory or contractual cap on a client's cumulative hours, in one of two mutually exclusive modes.
/// Fixed-period mode ("max 45 h overtime per month") uses <see cref="Period"/>/<see cref="Scope"/>/
/// <see cref="CapHours"/> and compares the total accrued within a calendar-anchored window against
/// CapHours. Rolling-average mode ("48 h/week averaged over 17 weeks", K6) uses
/// <see cref="RollingWindowWeeks"/>/<see cref="MaxAverageWeeklyHours"/> and compares the average weekly
/// hours over a trailing window ending on the evaluated day against MaxAverageWeeklyHours. Exactly one of
/// the two field groups is populated per row — <see cref="Klacks.Api.Infrastructure.Services.Settings.RegionSetupService"/>
/// validates this on import, <see cref="Klacks.Api.Application.Services.Schedules.PeriodCapEvaluator"/>
/// dispatches on which group is populated. Scope: with <see cref="SchedulingRuleId"/> null the rule is
/// GLOBAL; otherwise it applies only to clients whose active contract references that scheduling rule
/// (the industry axis — industryProfiles imports bind their caps to the block's rule preset). Several
/// rules can coexist. Rows are written exclusively by the region-setup entity-import mechanism (K20);
/// ImportSourceKey/ImportContentHash (see <see cref="Klacks.Api.Domain.Common.IImportableEntity"/>) drive
/// that re-apply logic.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public class PeriodCapRule : BaseEntity, IImportableEntity
{
    public PeriodCapPeriod Period { get; set; }

    public PeriodCapScope Scope { get; set; }

    public decimal CapHours { get; set; }

    public int? WarnAtPercent { get; set; }

    /// <summary>
    /// Number of trailing weeks the window covers when <see cref="Period"/> is CustomWeeks. Unused for
    /// Month/Quarter/Year. CustomWeeks is not yet importable via region-setup.json (see PeriodCapPeriod).
    /// </summary>
    public int? CustomPeriodWeeks { get; set; }

    /// <summary>
    /// K6 rolling-average mode: width in weeks of the trailing window ending on the evaluated day. Set
    /// together with <see cref="MaxAverageWeeklyHours"/>; both null means this row is a fixed-period rule.
    /// </summary>
    public int? RollingWindowWeeks { get; set; }

    /// <summary>
    /// K6 rolling-average mode: the average weekly hours threshold over <see cref="RollingWindowWeeks"/>
    /// that must not be exceeded. Set together with <see cref="RollingWindowWeeks"/>.
    /// </summary>
    public decimal? MaxAverageWeeklyHours { get; set; }

    public Guid? SchedulingRuleId { get; set; }

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
