// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Changes how strictly the working-time protections are applied. Only the supplied parameters are
/// written; the rest keep their stored value. Every reaction (*Mode) must be warn or block and is stored in that
/// spelling; any other value refuses the whole call without writing.
/// </summary>
/// <param name="defaultMode">Reaction used where no specific mode is set.</param>
/// <param name="allowSupervisorOverride">Whether a supervisor may push a rejected assignment through.</param>
/// <param name="maxDailyHours">Hours a single day may reach.</param>
/// <param name="maxWeeklyHours">Hours a single week may reach.</param>
/// <param name="minRestHours">Hours of rest between two duties.</param>
/// <param name="minRestDays">Rest days per week.</param>
/// <param name="maxConsecutiveDays">Days in a row that may be worked.</param>
/// <param name="periodCapMode">Reaction when a period ceiling is passed.</param>
/// <param name="rollingAverageMode">Reaction when a rolling weekly average is passed.</param>
/// <param name="restDayRotationMode">Reaction when the rest day rotation is broken.</param>
/// <param name="counterRuleMode">Reaction when a counting rule is exceeded.</param>
/// <param name="compensatoryRestMode">Reaction when owed time off is not granted in time.</param>
/// <param name="restrictedTimeWindowMode">Reaction when a blocked stretch of the day is used.</param>
/// <param name="holidayWorkMode">Reaction to work on a statutory holiday of the contract calendar (warn or block); refused when it is neither.</param>
/// <param name="rosterPublicationMinLeadDays">Days a roster has to be published ahead.</param>
/// <param name="rosterPublicationCountWorkdaysOnly">Whether that lead time counts working days only.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Skills.Base;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_compliance_enforcement_settings")]
public class UpdateComplianceEnforcementSettingsSkill : SettingsWriterSkillBase
{
    private static readonly (string Parameter, string Key)[] ModeParameters =
    [
        ("defaultMode", SettingKeys.ComplianceEnforcementDefaultMode),
        ("periodCapMode", SettingKeys.ComplianceEnforcementPeriodCap),
        ("rollingAverageMode", SettingKeys.ComplianceEnforcementRollingAverage),
        ("restDayRotationMode", SettingKeys.ComplianceEnforcementRestDayRotation),
        ("counterRuleMode", SettingKeys.ComplianceEnforcementCounterRule),
        ("compensatoryRestMode", SettingKeys.ComplianceEnforcementCompensatoryRest),
        ("restrictedTimeWindowMode", SettingKeys.ComplianceEnforcementRestrictedTimeWindow),
        ("holidayWorkMode", SettingKeys.ComplianceEnforcementHolidayWork),
    ];

    public UpdateComplianceEnforcementSettingsSkill(
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

        foreach (var (parameter, key) in ModeParameters)
        {
            var raw = GetParameter<string>(parameters, parameter);
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var normalized = ComplianceEnforcementModeValues.Normalize(raw);
            if (normalized == null)
            {
                return SkillResult.Error(
                    $"{parameter} must be '{ComplianceEnforcementModeValues.Warn}' or " +
                    $"'{ComplianceEnforcementModeValues.Block}' - nothing was changed.");
            }

            pending.Add(new PendingSetting(parameter, key, normalized));
        }

        CollectBoolean(pending, parameters, "allowSupervisorOverride", SettingKeys.ComplianceEnforcementAllowSupervisorOverride);
        CollectDecimal(pending, parameters, "maxDailyHours", SettingKeys.ComplianceEnforcementMaxDailyHours);
        CollectDecimal(pending, parameters, "maxWeeklyHours", SettingKeys.ComplianceEnforcementMaxWeeklyHours);
        CollectDecimal(pending, parameters, "minRestHours", SettingKeys.ComplianceEnforcementMinRestHours);
        CollectInteger(pending, parameters, "minRestDays", SettingKeys.ComplianceEnforcementMinRestDays);
        CollectInteger(pending, parameters, "maxConsecutiveDays", SettingKeys.ComplianceEnforcementMaxConsecutiveDays);
        CollectInteger(pending, parameters, "rosterPublicationMinLeadDays", SettingKeys.ComplianceRosterPublicationMinLeadDays);
        CollectBoolean(pending, parameters, "rosterPublicationCountWorkdaysOnly", SettingKeys.ComplianceRosterPublicationCountWorkdaysOnly);

        return await PersistAsync(pending, "Compliance enforcement settings");
    }
}