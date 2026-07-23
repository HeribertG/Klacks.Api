// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A named rule set overriding the global scheduling/surcharge settings for the contracts that
/// reference it (Contract.SchedulingRuleId). Rows are either customer-created (full CRUD,
/// ImportSourceKey empty) or imported as industry presets by the region-setup entity-import path (K20,
/// ImportSourceKey/ImportContentHash set — see <see cref="Klacks.Api.Domain.Common.IImportableEntity"/>);
/// an imported row a customer has edited is never overwritten by a re-import.
/// </summary>

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Scheduling;

public class SchedulingRule : BaseEntity, IImportableEntity
{
    public string Name { get; set; } = string.Empty;

    public int? MaxWorkDays { get; set; }

    public decimal? MinRestDays { get; set; }

    public decimal? MinPauseHours { get; set; }

    public decimal? MaxOptimalGap { get; set; }

    public decimal? MaxDailyHours { get; set; }

    public decimal? MaxWeeklyHours { get; set; }

    public int? MaxConsecutiveDays { get; set; }

    public decimal? DefaultWorkingHours { get; set; }

    public decimal? OvertimeThreshold { get; set; }

    public decimal? GuaranteedHours { get; set; }

    public decimal? MaximumHours { get; set; }

    public decimal? MinimumHours { get; set; }

    public decimal? FullTimeHours { get; set; }

    public int? VacationDaysPerYear { get; set; }

    public decimal? NightRate { get; set; }

    public decimal? HolidayRate { get; set; }

    public decimal? WE1Rate { get; set; }

    public decimal? WE2Rate { get; set; }

    public decimal? WE3Rate { get; set; }

    public string? NightStart { get; set; }

    public string? NightEnd { get; set; }

    public bool? WorkOnMonday { get; set; }

    public bool? WorkOnTuesday { get; set; }

    public bool? WorkOnWednesday { get; set; }

    public bool? WorkOnThursday { get; set; }

    public bool? WorkOnFriday { get; set; }

    public bool? WorkOnSaturday { get; set; }

    public bool? WorkOnSunday { get; set; }

    public bool? PerformsShiftWork { get; set; }

    public OvertimeBasis? OvertimeBasis { get; set; }

    public SurchargeRateMode? OvertimeRateMode { get; set; }

    public decimal? OvertimeTier1AfterHours { get; set; }

    public decimal? OvertimeTier1Rate { get; set; }

    public decimal? OvertimeTier2AfterHours { get; set; }

    public decimal? OvertimeTier2Rate { get; set; }

    public decimal? OvertimeTier3AfterHours { get; set; }

    public decimal? OvertimeTier3Rate { get; set; }

    public string Industry { get; set; } = string.Empty;

    public string ImportSourceKey { get; set; } = string.Empty;

    public string ImportContentHash { get; set; } = string.Empty;

    public ICollection<SchedulingRuleRateRevision> RateRevisions { get; set; } = new List<SchedulingRuleRateRevision>();
}
