// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores how many days after a period ends it is closed (PERIOD_CLOSE_LAG_DAYS). The stored value is the
/// proof that the user was asked when periods are to be closed: without it Klacksy never closes a period on
/// its own. It moves the reminders and, only at the autonomy level FullyAutonomous, the automatic close.
/// The user's consent is not asked for in this class: the skill is classified Sensitive, so the confirmation
/// gate holds every call until the user has said yes.
/// </summary>
/// <param name="settingsRepository">Reads and, through the base class, writes the setting</param>
/// <param name="unitOfWork">Commits the write</param>
/// <param name="encryptionService">Storage transform of the settings store</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("set_period_close_lag")]
public class SetPeriodCloseLagSkill : SettingsWriterSkillBase
{
    public SetPeriodCloseLagSkill(
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
        var lag = GetParameter<int?>(parameters, PeriodCloseParameters.LagDays);
        if (lag is null)
        {
            return SkillResult.Error($"Missing required value: {PeriodCloseParameters.LagDays}.");
        }

        if (!PeriodCloseDateCalculator.IsValidLag(lag.Value))
        {
            return SkillResult.Error(
                $"{PeriodCloseParameters.LagDays} must be a whole number of days between "
                + $"{PeriodCloseDateCalculator.MinLagDays} and {PeriodCloseDateCalculator.MaxLagDays}.");
        }

        var pending = new List<PendingSetting>
        {
            new(PeriodCloseParameters.LagDays, SettingKeys.PeriodCloseLagDays,
                lag.Value.ToString(CultureInfo.InvariantCulture))
        };

        return await PersistAsync(pending, $"Period close lag ({lag.Value} days after the period end)");
    }
}
