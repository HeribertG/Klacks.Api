// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes how the extra-pay kinds are calculated. Only the supplied parameters are written; the
/// rest keep their stored value.
/// </summary>
/// <param name="nightRateMode">Whether night pay is a multiplier or a fixed amount.</param>
/// <param name="holidayRateMode">Whether holiday pay is a multiplier or a fixed amount.</param>
/// <param name="we1RateMode">Whether Saturday pay is a multiplier or a fixed amount.</param>
/// <param name="we2RateMode">Whether Sunday pay is a multiplier or a fixed amount.</param>
/// <param name="we3RateMode">Whether the third weekend band is a multiplier or a fixed amount.</param>
/// <param name="nightMinimumPerHour">Guaranteed minimum per night hour.</param>
/// <param name="holidayMinimumPerHour">Guaranteed minimum per holiday hour.</param>
/// <param name="we1MinimumPerHour">Guaranteed minimum per Saturday hour.</param>
/// <param name="we2MinimumPerHour">Guaranteed minimum per Sunday hour.</param>
/// <param name="we3MinimumPerHour">Guaranteed minimum per hour in the third weekend band.</param>
/// <param name="nightStart">Hour the night band starts, as HH:mm.</param>
/// <param name="nightEnd">Hour the night band ends, as HH:mm.</param>
/// <param name="stackingMode">How several kinds combine when they meet on the same hour.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_surcharge_mode_settings")]
public class UpdateSurchargeModeSettingsSkill : SettingsWriterSkillBase
{
    public UpdateSurchargeModeSettingsSkill(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ISettingsEncryptionService encryptionService)
        : base(settingsRepository, unitOfWork, encryptionService)
    {
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var pending = new List<PendingSetting>();

        CollectText(pending, parameters, "nightRateMode", SettingKeys.SurchargeNightRateMode);
        CollectText(pending, parameters, "holidayRateMode", SettingKeys.SurchargeHolidayRateMode);
        CollectText(pending, parameters, "we1RateMode", SettingKeys.SurchargeWE1RateMode);
        CollectText(pending, parameters, "we2RateMode", SettingKeys.SurchargeWE2RateMode);
        CollectText(pending, parameters, "we3RateMode", SettingKeys.SurchargeWE3RateMode);
        CollectDecimal(pending, parameters, "nightMinimumPerHour", SettingKeys.SurchargeNightMinimumPerHour);
        CollectDecimal(pending, parameters, "holidayMinimumPerHour", SettingKeys.SurchargeHolidayMinimumPerHour);
        CollectDecimal(pending, parameters, "we1MinimumPerHour", SettingKeys.SurchargeWE1MinimumPerHour);
        CollectDecimal(pending, parameters, "we2MinimumPerHour", SettingKeys.SurchargeWE2MinimumPerHour);
        CollectDecimal(pending, parameters, "we3MinimumPerHour", SettingKeys.SurchargeWE3MinimumPerHour);
        CollectText(pending, parameters, "nightStart", SettingKeys.SurchargeNightStart);
        CollectText(pending, parameters, "nightEnd", SettingKeys.SurchargeNightEnd);
        CollectText(pending, parameters, "stackingMode", SettingKeys.SurchargeStackingMode);

        return await PersistAsync(pending, "Surcharge mode settings");
    }
}
