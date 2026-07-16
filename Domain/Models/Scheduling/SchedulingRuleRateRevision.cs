// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A dated full-snapshot revision of the surcharge rates and overtime tiers of a <see cref="SchedulingRule"/>
/// that takes effect from <see cref="ValidFrom"/> onward. Evaluated per work date at the
/// effective-contract-data chokepoint (rates) and the OvertimeSurchargeCalculator chokepoint (tiers): the
/// latest revision with ValidFrom &lt;= work date replaces the rule's base columns entirely (a null field
/// falls through to contract/settings, it does NOT inherit from the base rule or an earlier revision — so
/// an applicable revision that omits the overtime block falls through to the global OVERTIME_* settings,
/// not the base rule ladder). The overtime columns mirror <see cref="SchedulingRule"/> exactly. Rows are
/// imported by the region-setup entity-import path (K20) and are protected against re-import overwrite via
/// <see cref="Klacks.Api.Domain.Common.IImportableEntity"/> independently of the parent rule.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public class SchedulingRuleRateRevision : BaseEntity, IImportableEntity
{
    public Guid SchedulingRuleId { get; set; }

    public SchedulingRule? SchedulingRule { get; set; }

    public DateOnly ValidFrom { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? WE1Rate { get; set; }

    public decimal? WE2Rate { get; set; }

    public decimal? WE3Rate { get; set; }

    public OvertimeBasis? OvertimeBasis { get; set; }

    public SurchargeRateMode? OvertimeRateMode { get; set; }

    public decimal? OvertimeTier1AfterHours { get; set; }

    public decimal? OvertimeTier1Rate { get; set; }

    public decimal? OvertimeTier2AfterHours { get; set; }

    public decimal? OvertimeTier2Rate { get; set; }

    public decimal? OvertimeTier3AfterHours { get; set; }

    public decimal? OvertimeTier3Rate { get; set; }

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
