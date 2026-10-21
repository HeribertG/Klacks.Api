// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Validates the locale, calendar, worktime, export, activeIndustries and package sections of a
/// region-setup profile and appends the resulting global settings. Pure and fail-fast: an invalid value
/// throws before any write.
/// </summary>
/// <param name="settings">Planned (type, value) setting writes a section is appended to</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

internal static class RegionSetupSettingsBuilder
{
    private const string ListSeparator = ",";
    private const int PackageCountryCodeLength = 2;

    internal static void AddLocaleSettings(RegionSetupLocale? locale, List<(string Type, string Value)> settings)
    {
        if (locale == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(locale.TimeZone))
        {
            RegionSetupFieldParser.ValidateTimeZone(locale.TimeZone);
            settings.Add((Application.Constants.Settings.APP_ADDRESS_TIMEZONE, locale.TimeZone));
        }

        if (!string.IsNullOrWhiteSpace(locale.Country))
        {
            settings.Add((Application.Constants.Settings.APP_ADDRESS_COUNTRY, locale.Country));
            settings.Add((SettingKeys.GlobalCalendarCountry, locale.Country));
        }

        if (!string.IsNullOrWhiteSpace(locale.State))
        {
            settings.Add((Application.Constants.Settings.APP_ADDRESS_STATE, locale.State));
            settings.Add((SettingKeys.GlobalCalendarState, locale.State));
        }

        if (locale.CalendarSelection != null && string.IsNullOrWhiteSpace(locale.CalendarSelection.Country))
        {
            throw new InvalidRequestException("Region setup: locale.calendarSelection requires a country.");
        }
    }

    internal static void AddCalendarSettings(RegionSetupCalendar? calendar, List<(string Type, string Value)> settings)
    {
        if (calendar == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(calendar.WeekendDays))
        {
            var days = calendar.WeekendDays
                .Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(day => RegionSetupFieldParser.ParseDayOfWeek(day, "calendar.weekendDays"))
                .ToList();

            if (days.Count == 0)
            {
                throw new InvalidRequestException("Region setup: calendar.weekendDays contains no day names.");
            }

            settings.Add((SettingKeys.WeekendDays, string.Join(ListSeparator, days)));
        }

        if (!string.IsNullOrWhiteSpace(calendar.WeekStartDay))
        {
            var day = RegionSetupFieldParser.ParseDayOfWeek(calendar.WeekStartDay.Trim(), "calendar.weekStartDay");
            settings.Add((SettingKeys.WeekStartDay, day.ToString()));
        }
    }

    internal static void AddWorktimeSettings(RegionSetupWorktime? worktime, List<(string Type, string Value)> settings)
    {
        if (worktime == null)
        {
            return;
        }

        if (!DailyWorkFrameLimits.IsInRange(worktime.MaxDailySpanHours))
        {
            throw new InvalidRequestException(
                $"Region setup: worktime.maxDailySpanHours must be between 0 and {DailyWorkFrameLimits.MaxHours} hours.");
        }

        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.MaximumHours, worktime.MaximumHours);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.MinimumHours, worktime.MinimumHours);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.FullTime, worktime.FullTime);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.GuaranteedHours, worktime.GuaranteedHours);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.DefaultWorkingHours, worktime.DefaultWorkingHours);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.OvertimeThreshold, worktime.OvertimeThreshold);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.VacationDaysPerYear, worktime.VacationDaysPerYear);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.SchedulingMaxDailyHours, worktime.MaxDailyHours);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.SchedulingMaxWeeklyHours, worktime.MaxWeeklyHours);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.SchedulingMaxConsecutiveDays, worktime.MaxConsecutiveDays);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.SchedulingMinRestDays, worktime.MinRestDays);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.SchedulingMinPauseHours, worktime.MinPauseHours);
        RegionSetupSettingAppender.AddNumber(settings, SettingKeys.SchedulingMaxDailySpanHours, worktime.MaxDailySpanHours);
    }

    internal static void AddExportSettings(RegionSetupExport? export, List<(string Type, string Value)> settings)
    {
        if (export == null)
        {
            return;
        }

        if (export.EnabledFormats != null)
        {
            if (export.EnabledFormats.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidRequestException("Region setup: export.enabledFormats contains an empty format id.");
            }

            settings.Add((SettingKeys.EnabledExportFormats, string.Join(ListSeparator, export.EnabledFormats.Select(format => format.Trim()))));
        }

        if (!string.IsNullOrWhiteSpace(export.DefaultPayrollTargetSystem))
        {
            settings.Add((SettingKeys.DefaultPayrollTargetSystem, export.DefaultPayrollTargetSystem));
        }
    }

    // Fail-fast validation of the industry arming list: every entry must resolve (via the same slug
    // normalization the importer applies to industryProfiles keys) to a key of the industryProfiles map
    // of the SAME file, the list must not be empty ("empty = all" is forbidden; omit the field instead),
    // no entry may equal the reserved IndustrySlugs.Custom marker (shipped content never opts an
    // installation into "custom", that is an admin-only choice made via the settings UI) and
    // industryProfiles must be present at all. Runs inside BuildPlan, ahead of any write.
    internal static void AddActiveIndustriesSettings(RegionSetupProfile profile, List<(string Type, string Value)> settings)
    {
        var activeIndustries = profile.ActiveIndustries;
        if (activeIndustries == null)
        {
            return;
        }

        if (activeIndustries.Count == 0)
        {
            throw new InvalidRequestException(
                "Region setup: activeIndustries must not be empty - omit the field to keep all industries active.");
        }

        if (profile.IndustryProfiles == null || profile.IndustryProfiles.Count == 0)
        {
            throw new InvalidRequestException(
                "Region setup: activeIndustries requires an industryProfiles map in the same file.");
        }

        var availableSlugs = profile.IndustryProfiles.Keys
            .Select(RegionSetupFieldParser.BuildImportSlug)
            .ToHashSet(StringComparer.Ordinal);

        var slugs = new List<string>();
        foreach (var raw in activeIndustries)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidRequestException("Region setup: activeIndustries contains an empty industry slug.");
            }

            var slug = RegionSetupFieldParser.BuildImportSlug(raw);
            if (slug == IndustrySlugs.Custom)
            {
                throw new InvalidRequestException(
                    $"Region setup: activeIndustries entry '{raw}' is reserved for the custom marker and must not appear in shipped region-setup content.");
            }

            if (!availableSlugs.Contains(slug))
            {
                throw new InvalidRequestException(
                    $"Region setup: activeIndustries entry '{raw}' matches no key of the industryProfiles map in the same file.");
            }

            if (slugs.Contains(slug))
            {
                throw new InvalidRequestException($"Region setup: activeIndustries contains duplicate entry '{raw}'.");
            }

            slugs.Add(slug);
        }

        settings.Add((SettingKeys.ActiveIndustries, string.Join(ListSeparator, slugs)));
    }

    // Both fields are mandatory when the block is present; the country must be a two-letter ISO code
    // (normalized to lowercase, matching the region profile file names). The resulting settings are
    // planned on EVERY run and never marker-gated: a newer downloaded package version must replace the
    // recorded identity.
    internal static void AddPackageIdentitySettings(RegionSetupPackage? package, List<(string Type, string Value)> settings)
    {
        if (package == null)
        {
            return;
        }

        var country = package.Country?.Trim() ?? string.Empty;
        if (country.Length != PackageCountryCodeLength || !country.All(char.IsAsciiLetter))
        {
            throw new InvalidRequestException(
                $"Region setup: package.country '{package.Country}' must be a two-letter ISO country code.");
        }

        if (string.IsNullOrWhiteSpace(package.Version))
        {
            throw new InvalidRequestException("Region setup: package.version must be a non-empty string.");
        }

        settings.Add((SettingKeys.RegionPackageCountry, country.ToLowerInvariant()));
        settings.Add((SettingKeys.RegionPackageVersion, package.Version.Trim()));
    }
}
