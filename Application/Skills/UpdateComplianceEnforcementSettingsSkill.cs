// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Changes how strictly the working-time protections are applied. Only the supplied parameters are
/// written; the rest keep their stored value. Every reaction (*Mode) must be warn or block and is stored in that
/// spelling; any other value refuses the whole call without writing. The COMPLIANCE_ENFORCEMENT_* key of each rule
/// holds only the reaction, so this skill takes no hour or day numbers.
/// </summary>
/// <param name="defaultMode">Reaction used where no specific mode is set.</param>
/// <param name="allowSupervisorOverride">Whether a supervisor may push a rejected assignment through.</param>
/// <param name="maxDailyHoursMode">Reaction when the daily hours limit is passed.</param>
/// <param name="maxWeeklyHoursMode">Reaction when the weekly hours limit is passed.</param>
/// <param name="minRestHoursMode">Reaction when the rest between two duties is too short.</param>
/// <param name="minRestDaysMode">Reaction when the weekly rest days are missing.</param>
/// <param name="maxConsecutiveDaysMode">Reaction when too many days in a row are worked.</param>
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
        ("maxDailyHoursMode", SettingKeys.ComplianceEnforcementMaxDailyHours),
        ("maxWeeklyHoursMode", SettingKeys.ComplianceEnforcementMaxWeeklyHours),
        ("minRestHoursMode", SettingKeys.ComplianceEnforcementMinRestHours),
        ("minRestDaysMode", SettingKeys.ComplianceEnforcementMinRestDays),
        ("maxConsecutiveDaysMode", SettingKeys.ComplianceEnforcementMaxConsecutiveDays),
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
        CollectInteger(pending, parameters, "rosterPublicationMinLeadDays", SettingKeys.ComplianceRosterPublicationMinLeadDays);
        CollectBoolean(pending, parameters, "rosterPublicationCountWorkdaysOnly", SettingKeys.ComplianceRosterPublicationCountWorkdaysOnly);

        return await PersistAsync(pending, "Compliance enforcement settings");
    }
}