// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates the settings part of the compliance section of a region-setup profile (qualification
/// expiry, enforcement modes, roster publication lead time, compensatory rest) and appends the resulting
/// global settings. The entity rows of the section (period caps, rest-day rotations, counter rules,
/// restricted time windows) are imported by their own importers, not here.
/// </summary>
/// <param name="compliance">Compliance section of the profile; null adds nothing</param>
/// <param name="settings">Planned (type, value) setting writes the section is appended to</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class RegionSetupComplianceSettingsBuilder
{
    internal static void AddComplianceSettings(RegionSetupCompliance? compliance, List<(string Type, string Value)> settings)
    {
        if (compliance == null)
        {
            return;
        }

        AddQualificationComplianceSettings(compliance.Qualifications, settings);
        AddEnforcementSettings(compliance.Enforcement, settings);
        AddRosterPublicationSettings(compliance.RosterPublication, settings);
        AddCompensatoryRestSettings(compliance.CompensatoryRest, settings);
    }

    internal static void AddCompensatoryRestSettings(RegionSetupCompensatoryRest? compensatoryRest, List<(string Type, string Value)> settings)
    {
        if (compensatoryRest == null)
        {
            return;
        }

        if (compensatoryRest.DeadlineDays is null or <= 0)
        {
            throw new InvalidRequestException(
                "Region setup: compliance.compensatoryRest.deadlineDays is required and must be greater than zero.");
        }

        RegionSetupSettingAppender.AddBool(settings, SettingKeys.ComplianceCompensatoryRestEnabled, compensatoryRest.Enabled);
        RegionSetupSettingAppender.AddInt(settings, SettingKeys.ComplianceCompensatoryRestDeadlineDays, compensatoryRest.DeadlineDays);
    }

    private static void AddQualificationComplianceSettings(RegionSetupQualifications? qualifications, List<(string Type, string Value)> settings)
    {
        if (qualifications == null)
        {
            return;
        }

        RegionSetupSettingAppender.AddBool(settings, SettingKeys.QualificationExpiredMandatoryBlocks, qualifications.ExpiredMandatoryBlocks);
        RegionSetupSettingAppender.AddInt(settings, SettingKeys.QualificationExpiryWarningDays, qualifications.ExpiryWarningDays);
    }

    private static void AddEnforcementSettings(RegionSetupEnforcement? enforcement, List<(string Type, string Value)> settings)
    {
        if (enforcement == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(enforcement.DefaultMode))
        {
            settings.Add((SettingKeys.ComplianceEnforcementDefaultMode, ValidateEnforcementMode(enforcement.DefaultMode, "compliance.enforcement.defaultMode")));
        }

        RegionSetupSettingAppender.AddBool(settings, SettingKeys.ComplianceEnforcementAllowSupervisorOverride, enforcement.AllowSupervisorOverride);

        var rules = enforcement.Rules;
        if (rules == null)
        {
            return;
        }

        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementMaxDailyHours, rules.MaxDailyHours, "compliance.enforcement.rules.maxDailyHours");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementMaxWeeklyHours, rules.MaxWeeklyHours, "compliance.enforcement.rules.maxWeeklyHours");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementMinRestHours, rules.MinRestHours, "compliance.enforcement.rules.minRestHours");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementMinRestDays, rules.MinRestDays, "compliance.enforcement.rules.minRestDays");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementMaxConsecutiveDays, rules.MaxConsecutiveDays, "compliance.enforcement.rules.maxConsecutiveDays");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementPeriodCap, rules.PeriodCap, "compliance.enforcement.rules.periodCap");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementRollingAverage, rules.RollingAverage, "compliance.enforcement.rules.rollingAverage");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementRestDayRotation, rules.RestDayRotation, "compliance.enforcement.rules.restDayRotation");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementCounterRule, rules.CounterRule, "compliance.enforcement.rules.counterRule");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementCompensatoryRest, rules.CompensatoryRest, "compliance.enforcement.rules.compensatoryRest");
        AddEnforcementRule(settings, SettingKeys.ComplianceEnforcementRestrictedTimeWindow, rules.RestrictedTimeWindow, "compliance.enforcement.rules.restrictedTimeWindow");
    }

    private static void AddEnforcementRule(List<(string Type, string Value)> settings, string settingKey, string? mode, string fieldName)
    {
        if (!string.IsNullOrWhiteSpace(mode))
        {
            settings.Add((settingKey, ValidateEnforcementMode(mode, fieldName)));
        }
    }

    private static string ValidateEnforcementMode(string value, string fieldName)
    {
        return ComplianceEnforcementModeValues.Normalize(value)
               ?? throw new InvalidRequestException(
                   $"Region setup: '{value}' in {fieldName} must be '{ComplianceEnforcementModeValues.Warn}' or '{ComplianceEnforcementModeValues.Block}'.");
    }

    private static void AddRosterPublicationSettings(RegionSetupRosterPublication? rosterPublication, List<(string Type, string Value)> settings)
    {
        if (rosterPublication == null)
        {
            return;
        }

        RegionSetupSettingAppender.AddInt(settings, SettingKeys.ComplianceRosterPublicationMinLeadDays, rosterPublication.MinLeadDays);
        RegionSetupSettingAppender.AddBool(settings, SettingKeys.ComplianceRosterPublicationCountWorkdaysOnly, rosterPublication.CountWorkdaysOnly);
    }
}
