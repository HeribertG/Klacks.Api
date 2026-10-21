// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// K20 entity import of SchedulingRule preset rows from industryProfiles.*.schedulingRulePresets:
/// validates the presets into desired rows, plans insert/update/skip against the stored rows and applies
/// the decisions inside the import transaction, reporting the pre-existing rules whose surcharge fields
/// changed. Keep SchedulingRulePresetImportValues, SchedulingRulePresetContentHasher and
/// CopyPresetValues/ToImportValues here in sync - a field present in only two of the three silently
/// breaks the customer-edit detection.
/// </summary>
/// <param name="schedulingRuleImportRepository">Reads and stages imported SchedulingRule rows</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class SchedulingRulePresetImporter
{
    private readonly ISchedulingRuleImportRepository _schedulingRuleImportRepository;
    private readonly ILogger<SchedulingRulePresetImporter> _logger;

    public SchedulingRulePresetImporter(ISchedulingRuleImportRepository schedulingRuleImportRepository, ILogger<SchedulingRulePresetImporter> logger)
    {
        _schedulingRuleImportRepository = schedulingRuleImportRepository;
        _logger = logger;
    }

    internal static EntityImportDesired<SchedulingRulePresetImportValues> BuildDesired(
        string industry, RegionSetupSchedulingRulePreset preset, HashSet<string> seenKeys)
    {
        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            throw new InvalidRequestException(
                $"Region setup: industryProfiles.{industry}.schedulingRulePresets entry requires a non-empty 'name'.");
        }

        var fieldPrefix = $"industryProfiles.{industry}.schedulingRulePresets";
        RegionSetupFieldParser.RequireNonNegative(preset.MaxWorkDays, $"{fieldPrefix}.maxWorkDays");
        RegionSetupFieldParser.RequireNonNegative(preset.MinRestDays, $"{fieldPrefix}.minRestDays");
        RegionSetupFieldParser.RequireNonNegative(preset.MinPauseHours, $"{fieldPrefix}.minPauseHours");
        if (!DailyWorkFrameLimits.IsInRange(preset.MaxDailySpanHours))
        {
            throw new InvalidRequestException(
                $"Region setup: {fieldPrefix}.maxDailySpanHours must be between 0 and {DailyWorkFrameLimits.MaxHours} hours.");
        }
        RegionSetupFieldParser.RequireNonNegative(preset.MaxOptimalGap, $"{fieldPrefix}.maxOptimalGap");
        RegionSetupFieldParser.RequireNonNegative(preset.MaxDailyHours, $"{fieldPrefix}.maxDailyHours");
        RegionSetupFieldParser.RequireNonNegative(preset.MaxWeeklyHours, $"{fieldPrefix}.maxWeeklyHours");
        RegionSetupFieldParser.RequireNonNegative(preset.MaxConsecutiveDays, $"{fieldPrefix}.maxConsecutiveDays");
        RegionSetupFieldParser.RequireNonNegative(preset.DefaultWorkingHours, $"{fieldPrefix}.defaultWorkingHours");
        RegionSetupFieldParser.RequireNonNegative(preset.OvertimeThreshold, $"{fieldPrefix}.overtimeThreshold");
        RegionSetupFieldParser.RequireNonNegative(preset.GuaranteedHours, $"{fieldPrefix}.guaranteedHours");
        RegionSetupFieldParser.RequireNonNegative(preset.MaximumHours, $"{fieldPrefix}.maximumHours");
        RegionSetupFieldParser.RequireNonNegative(preset.MinimumHours, $"{fieldPrefix}.minimumHours");
        RegionSetupFieldParser.RequireNonNegative(preset.FullTimeHours, $"{fieldPrefix}.fullTimeHours");
        RegionSetupFieldParser.RequireNonNegative(preset.VacationDaysPerYear, $"{fieldPrefix}.vacationDaysPerYear");
        RegionSetupFieldParser.RequireNonNegative(preset.NightRate, $"{fieldPrefix}.nightRate");
        RegionSetupFieldParser.RequireNonNegative(preset.HolidayRate, $"{fieldPrefix}.holidayRate");
        RegionSetupFieldParser.RequireNonNegative(preset.We1Rate, $"{fieldPrefix}.we1Rate");
        RegionSetupFieldParser.RequireNonNegative(preset.We2Rate, $"{fieldPrefix}.we2Rate");
        RegionSetupFieldParser.RequireNonNegative(preset.We3Rate, $"{fieldPrefix}.we3Rate");

        if (!string.IsNullOrWhiteSpace(preset.NightStart))
        {
            RegionSetupFieldParser.ValidateTimeOfDay(preset.NightStart!, $"{fieldPrefix}.nightStart");
        }

        if (!string.IsNullOrWhiteSpace(preset.NightEnd))
        {
            RegionSetupFieldParser.ValidateTimeOfDay(preset.NightEnd!, $"{fieldPrefix}.nightEnd");
        }

        var overtime = PresetOvertimeParser.Parse(preset.Overtime, fieldPrefix);

        var name = preset.Name!.Trim();
        var sourceKey = $"region-setup:industryProfiles:{industry}:rule:{RegionSetupFieldParser.BuildImportSlug(name)}";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: industryProfiles contains more than one scheduling rule preset resolving to key '{sourceKey}'.");
        }

        var values = new SchedulingRulePresetImportValues(
            name,
            preset.MaxWorkDays,
            preset.MinRestDays,
            preset.MinPauseHours,
            preset.MaxOptimalGap,
            preset.MaxDailyHours,
            preset.MaxWeeklyHours,
            preset.MaxConsecutiveDays,
            preset.DefaultWorkingHours,
            preset.OvertimeThreshold,
            preset.GuaranteedHours,
            preset.MaximumHours,
            preset.MinimumHours,
            preset.FullTimeHours,
            preset.VacationDaysPerYear,
            preset.NightRate,
            preset.HolidayRate,
            preset.We1Rate,
            preset.We2Rate,
            preset.We3Rate,
            string.IsNullOrWhiteSpace(preset.NightStart) ? null : preset.NightStart!.Trim(),
            string.IsNullOrWhiteSpace(preset.NightEnd) ? null : preset.NightEnd!.Trim(),
            preset.PerformsShiftWork,
            overtime.Basis,
            overtime.RateMode,
            overtime.Tier1AfterHours,
            overtime.Tier1Rate,
            overtime.Tier2AfterHours,
            overtime.Tier2Rate,
            overtime.Tier3AfterHours,
            overtime.Tier3Rate,
            industry,
            preset.MaxDailySpanHours);

        return new EntityImportDesired<SchedulingRulePresetImportValues>(
            sourceKey,
            SchedulingRulePresetContentHasher.Compute(values),
            values);
    }

    public async Task<EntityImportFamilyPlan<SchedulingRulePresetImportValues, SchedulingRule>> PlanAsync(
        IReadOnlyList<EntityImportDesired<SchedulingRulePresetImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return EntityImportFamilyPlan<SchedulingRulePresetImportValues, SchedulingRule>.Empty;
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _schedulingRuleImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => SchedulingRulePresetContentHasher.Compute(ToImportValues(r)) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return new EntityImportFamilyPlan<SchedulingRulePresetImportValues, SchedulingRule>(decisions, existingBySourceKey);
    }

    // Besides the source-key/rule-id map this reports the ids of PRE-EXISTING rules whose
    // surcharge-relevant fields (rates, night window, overtime configuration) actually changed.
    // Freshly inserted rules are not reported: no contract can reference them yet, so no persisted
    // work surcharge can depend on them. A no-op re-import reports nothing.
    public (Dictionary<string, Guid> RuleIdBySourceKey, List<Guid> ChangedSurchargeRuleIds) Apply(
        EntityImportFamilyPlan<SchedulingRulePresetImportValues, SchedulingRule> plan)
    {
        var ruleIdBySourceKey = new Dictionary<string, Guid>();
        var changedSurchargeRuleIds = new List<Guid>();

        foreach (var decision in plan.Decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    var rule = new SchedulingRule { Id = Guid.NewGuid(), ImportSourceKey = decision.SourceKey };
                    CopyPresetValues(decision.Values, rule);
                    rule.ImportContentHash = decision.ContentHash;
                    _schedulingRuleImportRepository.Add(rule);
                    ruleIdBySourceKey[decision.SourceKey] = rule.Id;
                    break;
                case EntityImportAction.Update:
                    var existing = plan.ExistingBySourceKey[decision.SourceKey];
                    var surchargeFieldsChanged = PresetSurchargeFieldsDiffer(existing, decision.Values);
                    CopyPresetValues(decision.Values, existing);
                    existing.ImportContentHash = decision.ContentHash;
                    _schedulingRuleImportRepository.Update(existing);
                    ruleIdBySourceKey[decision.SourceKey] = existing.Id;
                    if (surchargeFieldsChanged)
                    {
                        changedSurchargeRuleIds.Add(existing.Id);
                    }
                    break;
                case EntityImportAction.SkipEdited:
                    ruleIdBySourceKey[decision.SourceKey] = plan.ExistingBySourceKey[decision.SourceKey].Id;
                    _logger.LogInformation(
                        "Region setup: scheduling rule preset '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }

        return (ruleIdBySourceKey, changedSurchargeRuleIds);
    }

    private static SchedulingRulePresetImportValues ToImportValues(SchedulingRule rule) => new(
        rule.Name,
        rule.MaxWorkDays,
        rule.MinRestDays,
        rule.MinPauseHours,
        rule.MaxOptimalGap,
        rule.MaxDailyHours,
        rule.MaxWeeklyHours,
        rule.MaxConsecutiveDays,
        rule.DefaultWorkingHours,
        rule.OvertimeThreshold,
        rule.GuaranteedHours,
        rule.MaximumHours,
        rule.MinimumHours,
        rule.FullTimeHours,
        rule.VacationDaysPerYear,
        rule.NightRate,
        rule.HolidayRate,
        rule.WE1Rate,
        rule.WE2Rate,
        rule.WE3Rate,
        rule.NightStart,
        rule.NightEnd,
        rule.PerformsShiftWork,
        rule.OvertimeBasis,
        rule.OvertimeRateMode,
        rule.OvertimeTier1AfterHours,
        rule.OvertimeTier1Rate,
        rule.OvertimeTier2AfterHours,
        rule.OvertimeTier2Rate,
        rule.OvertimeTier3AfterHours,
        rule.OvertimeTier3Rate,
        rule.Industry,
        rule.MaxDailySpanHours);

    private static bool PresetSurchargeFieldsDiffer(SchedulingRule existing, SchedulingRulePresetImportValues values)
    {
        return existing.NightRate != values.NightRate
            || existing.HolidayRate != values.HolidayRate
            || existing.WE1Rate != values.We1Rate
            || existing.WE2Rate != values.We2Rate
            || existing.WE3Rate != values.We3Rate
            || existing.NightStart != values.NightStart
            || existing.NightEnd != values.NightEnd
            || existing.OvertimeThreshold != values.OvertimeThreshold
            || existing.OvertimeBasis != values.OvertimeBasis
            || existing.OvertimeRateMode != values.OvertimeRateMode
            || existing.OvertimeTier1AfterHours != values.OvertimeTier1AfterHours
            || existing.OvertimeTier1Rate != values.OvertimeTier1Rate
            || existing.OvertimeTier2AfterHours != values.OvertimeTier2AfterHours
            || existing.OvertimeTier2Rate != values.OvertimeTier2Rate
            || existing.OvertimeTier3AfterHours != values.OvertimeTier3AfterHours
            || existing.OvertimeTier3Rate != values.OvertimeTier3Rate;
    }

    private static void CopyPresetValues(SchedulingRulePresetImportValues values, SchedulingRule rule)
    {
        rule.Name = values.Name;
        rule.MaxWorkDays = values.MaxWorkDays;
        rule.MinRestDays = values.MinRestDays;
        rule.MinPauseHours = values.MinPauseHours;
        rule.MaxOptimalGap = values.MaxOptimalGap;
        rule.MaxDailyHours = values.MaxDailyHours;
        rule.MaxWeeklyHours = values.MaxWeeklyHours;
        rule.MaxConsecutiveDays = values.MaxConsecutiveDays;
        rule.DefaultWorkingHours = values.DefaultWorkingHours;
        rule.OvertimeThreshold = values.OvertimeThreshold;
        rule.GuaranteedHours = values.GuaranteedHours;
        rule.MaximumHours = values.MaximumHours;
        rule.MinimumHours = values.MinimumHours;
        rule.FullTimeHours = values.FullTimeHours;
        rule.VacationDaysPerYear = values.VacationDaysPerYear;
        rule.NightRate = values.NightRate;
        rule.HolidayRate = values.HolidayRate;
        rule.WE1Rate = values.We1Rate;
        rule.WE2Rate = values.We2Rate;
        rule.WE3Rate = values.We3Rate;
        rule.NightStart = values.NightStart;
        rule.NightEnd = values.NightEnd;
        rule.PerformsShiftWork = values.PerformsShiftWork;
        rule.OvertimeBasis = values.OvertimeBasis;
        rule.OvertimeRateMode = values.OvertimeRateMode;
        rule.OvertimeTier1AfterHours = values.OvertimeTier1AfterHours;
        rule.OvertimeTier1Rate = values.OvertimeTier1Rate;
        rule.OvertimeTier2AfterHours = values.OvertimeTier2AfterHours;
        rule.OvertimeTier2Rate = values.OvertimeTier2Rate;
        rule.OvertimeTier3AfterHours = values.OvertimeTier3AfterHours;
        rule.OvertimeTier3Rate = values.OvertimeTier3Rate;
        rule.Industry = values.Industry;
        rule.MaxDailySpanHours = values.MaxDailySpanHours;
    }
}
