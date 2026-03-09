// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill zum Lesen der Scheduling-Standardeinstellungen.
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetSchedulingDefaultsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_scheduling_defaults";

    public override string Description =>
        "Retrieves scheduling default settings including working hours, overtime thresholds, " +
        "guaranteed/maximum/minimum hours, full-time hours, scheduling rule limits, and surcharge rates.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetSchedulingDefaultsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var defaultWorkingHours = await GetSettingValue(SettingKeys.DefaultWorkingHours);
        var overtimeThreshold = await GetSettingValue(SettingKeys.OvertimeThreshold);
        var guaranteedHours = await GetSettingValue(SettingKeys.GuaranteedHours);
        var maximumHours = await GetSettingValue(SettingKeys.MaximumHours);
        var minimumHours = await GetSettingValue(SettingKeys.MinimumHours);
        var fullTime = await GetSettingValue(SettingKeys.FullTime);
        var schedulingMaxWorkDays = await GetSettingValue(SettingKeys.SchedulingMaxWorkDays);
        var schedulingMinRestDays = await GetSettingValue(SettingKeys.SchedulingMinRestDays);
        var schedulingMinPauseHours = await GetSettingValue(SettingKeys.SchedulingMinPauseHours);
        var schedulingMaxOptimalGap = await GetSettingValue(SettingKeys.SchedulingMaxOptimalGap);
        var schedulingMaxDailyHours = await GetSettingValue(SettingKeys.SchedulingMaxDailyHours);
        var schedulingMaxWeeklyHours = await GetSettingValue(SettingKeys.SchedulingMaxWeeklyHours);
        var schedulingMaxConsecutiveDays = await GetSettingValue(SettingKeys.SchedulingMaxConsecutiveDays);
        var nightRate = await GetSettingValue(SettingKeys.NightRate);
        var holidayRate = await GetSettingValue(SettingKeys.HolidayRate);
        var saRate = await GetSettingValue(SettingKeys.SaRate);
        var soRate = await GetSettingValue(SettingKeys.SoRate);

        var resultData = new
        {
            DefaultWorkingHours = defaultWorkingHours,
            OvertimeThreshold = overtimeThreshold,
            GuaranteedHours = guaranteedHours,
            MaximumHours = maximumHours,
            MinimumHours = minimumHours,
            FullTime = fullTime,
            SchedulingMaxWorkDays = schedulingMaxWorkDays,
            SchedulingMinRestDays = schedulingMinRestDays,
            SchedulingMinPauseHours = schedulingMinPauseHours,
            SchedulingMaxOptimalGap = schedulingMaxOptimalGap,
            SchedulingMaxDailyHours = schedulingMaxDailyHours,
            SchedulingMaxWeeklyHours = schedulingMaxWeeklyHours,
            SchedulingMaxConsecutiveDays = schedulingMaxConsecutiveDays,
            NightRate = nightRate,
            HolidayRate = holidayRate,
            SaRate = saRate,
            SoRate = soRate
        };

        return SkillResult.SuccessResult(resultData,
            $"Scheduling defaults: DefaultHours={defaultWorkingHours}, MaxWeeklyHours={schedulingMaxWeeklyHours}, MaxConsecutiveDays={schedulingMaxConsecutiveDays}");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
