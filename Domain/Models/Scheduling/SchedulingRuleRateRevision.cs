// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A dated full-snapshot revision of the surcharge rates of a <see cref="SchedulingRule"/> that takes
/// effect from <see cref="ValidFrom"/> onward. Evaluated per work date at the effective-contract-data
/// chokepoint: the latest revision with ValidFrom &lt;= work date replaces the rule's base rate columns
/// entirely (a null rate field falls through to contract/settings, it does NOT inherit from the base
/// rule or an earlier revision). Rows are imported by the region-setup entity-import path (K20) and are
/// protected against re-import overwrite via <see cref="Klacks.Api.Domain.Common.IImportableEntity"/>
/// independently of the parent rule.
/// </summary>

using Klacks.Api.Domain.Common;

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

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;
}
