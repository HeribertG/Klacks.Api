// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IComplianceEnforcementResolver"/>. Reads the per-rule COMPLIANCE_ENFORCEMENT_&lt;RULE&gt;
/// setting first; falls back to COMPLIANCE_ENFORCEMENT_DEFAULT_MODE; falls back to Warn (today's
/// behavior) when neither is configured, so an installation that never touches this feature is
/// unaffected.
/// </summary>
/// <param name="settingsReader">Reads the per-rule and default enforcement-mode settings</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ComplianceEnforcementResolver : IComplianceEnforcementResolver
{
    private const string BlockValue = "block";

    private static readonly IReadOnlyDictionary<string, string> RuleSettingKeys = new Dictionary<string, string>
    {
        [ComplianceRuleNames.MaxDailyHours] = SettingKeys.ComplianceEnforcementMaxDailyHours,
        [ComplianceRuleNames.MaxWeeklyHours] = SettingKeys.ComplianceEnforcementMaxWeeklyHours,
        [ComplianceRuleNames.MinRestHours] = SettingKeys.ComplianceEnforcementMinRestHours,
        [ComplianceRuleNames.MinRestDays] = SettingKeys.ComplianceEnforcementMinRestDays,
        [ComplianceRuleNames.MaxConsecutiveDays] = SettingKeys.ComplianceEnforcementMaxConsecutiveDays,
        [ComplianceRuleNames.PeriodCap] = SettingKeys.ComplianceEnforcementPeriodCap,
        [ComplianceRuleNames.RollingAverage] = SettingKeys.ComplianceEnforcementRollingAverage,
        [ComplianceRuleNames.RestDayRotation] = SettingKeys.ComplianceEnforcementRestDayRotation,
    };

    private readonly ISettingsReader _settingsReader;

    public ComplianceEnforcementResolver(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<RuleEnforcementMode> GetModeAsync(string ruleName)
    {
        if (RuleSettingKeys.TryGetValue(ruleName, out var ruleKey))
        {
            var perRule = await _settingsReader.GetSetting(ruleKey);
            if (!string.IsNullOrWhiteSpace(perRule?.Value))
            {
                return ParseMode(perRule.Value);
            }
        }

        var fallback = await _settingsReader.GetSetting(SettingKeys.ComplianceEnforcementDefaultMode);
        return string.IsNullOrWhiteSpace(fallback?.Value) ? RuleEnforcementMode.Warn : ParseMode(fallback.Value);
    }

    public async Task<bool> IsSupervisorOverrideAllowedAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.ComplianceEnforcementAllowSupervisorOverride);
        return setting?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;
    }

    private static RuleEnforcementMode ParseMode(string value) =>
        string.Equals(value, BlockValue, StringComparison.OrdinalIgnoreCase)
            ? RuleEnforcementMode.Block
            : RuleEnforcementMode.Warn;
}
