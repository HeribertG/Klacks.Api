// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill zum Lesen der arbeitsbezogenen Einstellungen.
/// </summary>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class GetWorkSettingsSkill : BaseSkill
{
    private readonly ISettingsRepository _settingsRepository;

    public override string Name => "get_work_settings";

    public override string Description =>
        "Retrieves work-related settings including vacation days, probation period, notice period, " +
        "payment interval, surcharge rates (night, holiday, saturday, sunday), and day visibility ranges.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => new[] { "CanViewSettings" };

    public override IReadOnlyList<SkillParameter> Parameters => Array.Empty<SkillParameter>();

    public GetWorkSettingsSkill(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var vacationDaysPerYear = await GetSettingValue(SettingKeys.VacationDaysPerYear);
        var probationPeriod = await GetSettingValue(SettingKeys.ProbationPeriod);
        var noticePeriod = await GetSettingValue(SettingKeys.NoticePeriod);
        var paymentInterval = await GetSettingValue(SettingKeys.PaymentInterval);
        var nightRate = await GetSettingValue(SettingKeys.NightRate);
        var holidayRate = await GetSettingValue(SettingKeys.HolidayRate);
        var saRate = await GetSettingValue(SettingKeys.SaRate);
        var soRate = await GetSettingValue(SettingKeys.SoRate);
        var dayVisibleBefore = await GetSettingValue(SettingKeys.DayVisibleBefore);
        var dayVisibleAfter = await GetSettingValue(SettingKeys.DayVisibleAfter);

        var resultData = new
        {
            VacationDaysPerYear = vacationDaysPerYear,
            ProbationPeriod = probationPeriod,
            NoticePeriod = noticePeriod,
            PaymentInterval = paymentInterval,
            NightRate = nightRate,
            HolidayRate = holidayRate,
            SaRate = saRate,
            SoRate = soRate,
            DayVisibleBefore = dayVisibleBefore,
            DayVisibleAfter = dayVisibleAfter
        };

        return SkillResult.SuccessResult(resultData,
            $"Work settings: VacationDays={vacationDaysPerYear}, PaymentInterval={paymentInterval}, NightRate={nightRate}");
    }

    private async Task<string> GetSettingValue(string settingType)
    {
        var setting = await _settingsRepository.GetSetting(settingType);
        return setting?.Value ?? "";
    }
}
