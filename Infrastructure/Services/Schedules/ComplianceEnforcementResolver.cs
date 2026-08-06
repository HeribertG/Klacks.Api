// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IComplianceEnforcementResolver"/>. Reads the per-rule COMPLIANCE_ENFORCEMENT_&lt;RULE&gt;
/// setting first; falls back to COMPLIANCE_ENFORCEMENT_DEFAULT_MODE; falls back to Warn (today's
/// behavior) when neither is configured, so an installation that never touches this feature is
/// unaffected. Only "warn" and "block" are recognized values: an unrecognized per-rule value (e.g.
/// a typo like "bloc") falls back to the default mode instead of silently weakening to Warn, and an
/// unrecognized override flag falls back to its default (enabled) — both are logged as warnings.
/// </summary>
/// <param name="settingsReader">Reads the per-rule and default enforcement-mode settings</param>
/// <param name="logger">Logs unrecognized setting values so admins notice typos</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ComplianceEnforcementResolver : IComplianceEnforcementResolver
{
    private const string BlockValue = "block";
    private const string WarnValue = "warn";
    private const string UnrecognizedValueMessage =
        "Unrecognized value '{Value}' in setting {SettingKey}; falling back to {Fallback}";

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
        [ComplianceRuleNames.CounterRule] = SettingKeys.ComplianceEnforcementCounterRule,
        [ComplianceRuleNames.CompensatoryRest] = SettingKeys.ComplianceEnforcementCompensatoryRest,
        [ComplianceRuleNames.RestrictedTimeWindow] = SettingKeys.ComplianceEnforcementRestrictedTimeWindow,
        [ComplianceRuleNames.HolidayWork] = SettingKeys.ComplianceEnforcementHolidayWork,
    };

    private readonly ISettingsReader _settingsReader;
    private readonly ILogger<ComplianceEnforcementResolver> _logger;

    public ComplianceEnforcementResolver(
        ISettingsReader settingsReader,
        ILogger<ComplianceEnforcementResolver> logger)
    {
        _settingsReader = settingsReader;
        _logger = logger;
    }

    public async Task<RuleEnforcementMode> GetModeAsync(string ruleName)
    {
        if (RuleSettingKeys.TryGetValue(ruleName, out var ruleKey))
        {
            var perRule = await _settingsReader.GetSetting(ruleKey);
            if (!string.IsNullOrWhiteSpace(perRule?.Value))
            {
                var perRuleMode = TryParseMode(perRule.Value);
                if (perRuleMode is not null)
                {
                    return perRuleMode.Value;
                }

                _logger.LogWarning(UnrecognizedValueMessage, perRule.Value, ruleKey, "the default mode");
            }
        }

        var fallback = await _settingsReader.GetSetting(SettingKeys.ComplianceEnforcementDefaultMode);
        if (string.IsNullOrWhiteSpace(fallback?.Value))
        {
            return RuleEnforcementMode.Warn;
        }

        var defaultMode = TryParseMode(fallback.Value);
        if (defaultMode is null)
        {
            _logger.LogWarning(
                UnrecognizedValueMessage, fallback.Value, SettingKeys.ComplianceEnforcementDefaultMode, RuleEnforcementMode.Warn);
            return RuleEnforcementMode.Warn;
        }

        return defaultMode.Value;
    }

    public async Task<bool> IsSupervisorOverrideAllowedAsync()
    {
        var setting = await _settingsReader.GetSetting(SettingKeys.ComplianceEnforcementAllowSupervisorOverride);
        if (string.IsNullOrWhiteSpace(setting?.Value))
        {
            return true;
        }

        if (bool.TryParse(setting.Value, out var allowed))
        {
            return allowed;
        }

        _logger.LogWarning(
            UnrecognizedValueMessage, setting.Value, SettingKeys.ComplianceEnforcementAllowSupervisorOverride, "enabled");
        return true;
    }

    private static RuleEnforcementMode? TryParseMode(string value)
    {
        if (string.Equals(value, BlockValue, StringComparison.OrdinalIgnoreCase))
        {
            return RuleEnforcementMode.Block;
        }

        if (string.Equals(value, WarnValue, StringComparison.OrdinalIgnoreCase))
        {
            return RuleEnforcementMode.Warn;
        }

        return null;
    }
}
