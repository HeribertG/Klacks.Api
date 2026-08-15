// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Changes how hours beyond the agreed workload are paid. Only the supplied parameters are written;
/// the rest keep their stored value.
/// </summary>
/// <param name="basis">What the count is measured against.</param>
/// <param name="rateMode">Whether the rate is a multiplier or a fixed amount.</param>
/// <param name="tier1AfterHours">Hour the first step starts at.</param>
/// <param name="tier1Rate">Rate the first step pays.</param>
/// <param name="tier2AfterHours">Hour the second step starts at.</param>
/// <param name="tier2Rate">Rate the second step pays.</param>
/// <param name="tier3AfterHours">Hour the third step starts at.</param>
/// <param name="tier3Rate">Rate the third step pays.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_overtime_settings")]
public class UpdateOvertimeSettingsSkill : SettingsWriterSkillBase
{
    public UpdateOvertimeSettingsSkill(
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

        CollectText(pending, parameters, "basis", SettingKeys.OvertimeBasis);
        CollectText(pending, parameters, "rateMode", SettingKeys.OvertimeRateMode);
        CollectDecimal(pending, parameters, "tier1AfterHours", SettingKeys.OvertimeTier1AfterHours);
        CollectDecimal(pending, parameters, "tier1Rate", SettingKeys.OvertimeTier1Rate);
        CollectDecimal(pending, parameters, "tier2AfterHours", SettingKeys.OvertimeTier2AfterHours);
        CollectDecimal(pending, parameters, "tier2Rate", SettingKeys.OvertimeTier2Rate);
        CollectDecimal(pending, parameters, "tier3AfterHours", SettingKeys.OvertimeTier3AfterHours);
        CollectDecimal(pending, parameters, "tier3Rate", SettingKeys.OvertimeTier3Rate);

        return await PersistAsync(pending, "Overtime settings");
    }
}
