// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Permission to work on a statutory holiday. Without a matching row, work placed on a day the
/// client's calendar marks as an official holiday (CalendarRule.IsMandatory) is reported by
/// <see cref="Klacks.Api.Application.Services.Schedules.HolidayWorkEvaluator"/>. Care, security and
/// similar operations run legally on holidays, so the detector would otherwise flood them with one
/// finding per staffed shift on every holiday.
/// Scope mirrors <see cref="PeriodCapRule"/>: with <see cref="SchedulingRuleId"/> null the exemption
/// is GLOBAL; otherwise it applies only to clients whose active contract references that scheduling
/// rule — the industry axis, since industryProfiles imports bind their rows to the block's rule
/// preset. Rows are importable via the region-setup entity-import mechanism (K20);
/// ImportSourceKey/ImportContentHash (see <see cref="Klacks.Api.Domain.Common.IImportableEntity"/>)
/// drive that re-apply logic.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Scheduling;

public class HolidayWorkExemptionRule : BaseEntity, IImportableEntity
{
    /// <summary>
    /// Human-readable reason the exemption exists, e.g. the statute permitting holiday work.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Null makes the exemption global; otherwise it only covers clients whose active contract
    /// references this scheduling rule.
    /// </summary>
    public Guid? SchedulingRuleId { get; set; }

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
