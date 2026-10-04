// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// ImportContentHash of a scheduling-rule preset row. Used BOTH for the desired hash from the profile file AND for
/// recomputing a stored row's live-value hash, so both calls format every field identically. The daily work frame
/// is appended only when set: a preset without it keeps the hash it had before the field existed, so imported
/// rows are not misread as customer-edited.
/// </summary>
/// <param name="values">Editable value fields of the preset row</param>

namespace Klacks.Api.Application.Services.Setup;

public static class SchedulingRulePresetContentHasher
{
    public static string Compute(SchedulingRulePresetImportValues values)
    {
        var fields = new List<string>
        {
            values.Name,
            ImportFieldFormatter.Int(values.MaxWorkDays),
            ImportFieldFormatter.Decimal(values.MinRestDays),
            ImportFieldFormatter.Decimal(values.MinPauseHours),
            ImportFieldFormatter.Decimal(values.MaxOptimalGap),
            ImportFieldFormatter.Decimal(values.MaxDailyHours),
            ImportFieldFormatter.Decimal(values.MaxWeeklyHours),
            ImportFieldFormatter.Int(values.MaxConsecutiveDays),
            ImportFieldFormatter.Decimal(values.DefaultWorkingHours),
            ImportFieldFormatter.Decimal(values.OvertimeThreshold),
            ImportFieldFormatter.Decimal(values.GuaranteedHours),
            ImportFieldFormatter.Decimal(values.MaximumHours),
            ImportFieldFormatter.Decimal(values.MinimumHours),
            ImportFieldFormatter.Decimal(values.FullTimeHours),
            ImportFieldFormatter.Int(values.VacationDaysPerYear),
            ImportFieldFormatter.Decimal(values.NightRate),
            ImportFieldFormatter.Decimal(values.HolidayRate),
            ImportFieldFormatter.Decimal(values.We1Rate),
            ImportFieldFormatter.Decimal(values.We2Rate),
            ImportFieldFormatter.Decimal(values.We3Rate),
            values.NightStart ?? string.Empty,
            values.NightEnd ?? string.Empty,
            ImportFieldFormatter.Bool(values.PerformsShiftWork),
            values.OvertimeBasis?.ToString() ?? string.Empty,
            values.OvertimeRateMode?.ToString() ?? string.Empty,
            ImportFieldFormatter.Decimal(values.OvertimeTier1AfterHours),
            ImportFieldFormatter.Decimal(values.OvertimeTier1Rate),
            ImportFieldFormatter.Decimal(values.OvertimeTier2AfterHours),
            ImportFieldFormatter.Decimal(values.OvertimeTier2Rate),
            ImportFieldFormatter.Decimal(values.OvertimeTier3AfterHours),
            ImportFieldFormatter.Decimal(values.OvertimeTier3Rate),
        };

        if (values.MaxDailySpanHours.HasValue)
        {
            fields.Add(ImportFieldFormatter.Decimal(values.MaxDailySpanHours));
        }

        return ImportContentHasher.ComputeHash([.. fields]);
    }
}
