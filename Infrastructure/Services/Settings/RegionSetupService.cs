// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One-time first-run region setup: reads an optional mounted JSON profile, installs language plugins
/// and writes locale, calendar, worktime, surcharge and export settings, then records a marker setting
/// so the setup never runs twice. Any invalid profile content fails fast before the first write.
/// </summary>
/// <param name="configuration">App configuration providing the RegionSetup:File path</param>
/// <param name="languagePluginService">Installs the requested language plugins</param>
/// <param name="settingsRepository">Reads and upserts rows of the settings table</param>
/// <param name="calendarSelectionRepository">Resolves the global calendar selection by country/state</param>
/// <param name="unitOfWork">Persists all setting writes in one transaction</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionSetupService : IRegionSetupService
{
    public const string FileConfigKey = RegionSetupFileReader.FileConfigKey;

    private const string ListSeparator = ",";

    private readonly IConfiguration _configuration;
    private readonly ILanguagePluginService _languagePluginService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegionSetupService> _logger;

    public RegionSetupService(
        IConfiguration configuration,
        ILanguagePluginService languagePluginService,
        ISettingsRepository settingsRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegionSetupService> logger)
    {
        _configuration = configuration;
        _languagePluginService = languagePluginService;
        _settingsRepository = settingsRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ApplyAsync()
    {
        var filePath = RegionSetupFileReader.GetConfiguredPath(_configuration);
        if (filePath == null)
        {
            _logger.LogDebug("Region setup: no profile file configured, skipping");
            return;
        }

        var marker = await _settingsRepository.GetSetting(SettingKeys.RegionSetupApplied);
        if (marker != null)
        {
            _logger.LogInformation("Region setup already applied, skipping");
            return;
        }

        var content = await RegionSetupFileReader.ReadContentAsync(filePath);
        var profile = RegionSetupFileReader.Parse(content, filePath);
        var (languagesToInstall, plannedSettings) = BuildPlan(profile);

        await InstallLanguagesAsync(languagesToInstall);

        var calendarSelectionId = await ResolveCalendarSelectionIdAsync(profile.Locale?.CalendarSelection);
        if (calendarSelectionId.HasValue)
        {
            plannedSettings.Add((SettingKeys.GlobalCalendarSelectionId, calendarSelectionId.Value.ToString()));
        }

        var markerValue = ComputeSha256Hex(content);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (type, value) in plannedSettings)
            {
                await UpsertSettingAsync(type, value);
            }

            await UpsertSettingAsync(SettingKeys.RegionSetupApplied, markerValue);
            await _unitOfWork.CompleteAsync();
            return plannedSettings.Count;
        });

        _logger.LogInformation(
            "Region setup applied: region '{Region}', {LanguageCount} language plugin(s) installed, {SettingCount} setting(s) written",
            profile.Region,
            languagesToInstall.Count,
            plannedSettings.Count);
    }

    private (List<string> LanguagesToInstall, List<(string Type, string Value)> Settings) BuildPlan(RegionSetupProfile profile)
    {
        var settings = new List<(string Type, string Value)>();

        var languagesToInstall = ValidateLanguages(profile.Languages);
        AddDefaultLanguageSetting(profile.Languages, languagesToInstall, settings);
        AddLocaleSettings(profile.Locale, settings);
        AddCalendarSettings(profile.Calendar, settings);
        AddWorktimeSettings(profile.Worktime, settings);
        AddSurchargeSettings(profile.Surcharges, settings);
        AddExportSettings(profile.Export, settings);

        return (languagesToInstall, settings);
    }

    private List<string> ValidateLanguages(RegionSetupLanguages? languages)
    {
        var toInstall = new List<string>();
        if (languages?.Install == null)
        {
            return toInstall;
        }

        foreach (var raw in languages.Install)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidRequestException("Region setup: languages.install contains an empty language code.");
            }

            var code = raw.Trim().ToLowerInvariant();
            if (LanguagePluginConstants.CoreLanguages.Contains(code))
            {
                _logger.LogInformation("Region setup: language '{Code}' is a core language, skipping installation", code);
                continue;
            }

            if (_languagePluginService.GetPlugin(code) == null)
            {
                throw new InvalidRequestException($"Region setup: unknown language plugin code '{code}'. No plugin with this code was discovered.");
            }

            toInstall.Add(code);
        }

        return toInstall;
    }

    private void AddDefaultLanguageSetting(
        RegionSetupLanguages? languages,
        IReadOnlyCollection<string> languagesToInstall,
        List<(string Type, string Value)> settings)
    {
        if (string.IsNullOrWhiteSpace(languages?.Default))
        {
            return;
        }

        var code = languages.Default.Trim().ToLowerInvariant();
        var isValid = LanguagePluginConstants.CoreLanguages.Contains(code)
            || languagesToInstall.Contains(code)
            || _languagePluginService.GetPlugin(code) != null;

        if (!isValid)
        {
            throw new InvalidRequestException(
                $"Region setup: languages.default '{code}' is neither a core language, nor listed in languages.install, nor a discovered language plugin.");
        }

        settings.Add((SettingKeys.DefaultLanguage, code));
    }

    private static void AddLocaleSettings(RegionSetupLocale? locale, List<(string Type, string Value)> settings)
    {
        if (locale == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(locale.TimeZone))
        {
            ValidateTimeZone(locale.TimeZone);
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

    private static void ValidateTimeZone(string timeZone)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZone);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            throw new InvalidRequestException($"Region setup: invalid time zone '{timeZone}'.");
        }
    }

    private static void AddCalendarSettings(RegionSetupCalendar? calendar, List<(string Type, string Value)> settings)
    {
        if (calendar == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(calendar.WeekendDays))
        {
            var days = calendar.WeekendDays
                .Split(ListSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(day => ParseDayOfWeek(day, "calendar.weekendDays"))
                .ToList();

            if (days.Count == 0)
            {
                throw new InvalidRequestException("Region setup: calendar.weekendDays contains no day names.");
            }

            settings.Add((SettingKeys.WeekendDays, string.Join(ListSeparator, days)));
        }

        if (!string.IsNullOrWhiteSpace(calendar.WeekStartDay))
        {
            var day = ParseDayOfWeek(calendar.WeekStartDay.Trim(), "calendar.weekStartDay");
            settings.Add((SettingKeys.WeekStartDay, day.ToString()));
        }
    }

    private static DayOfWeek ParseDayOfWeek(string value, string fieldName)
    {
        if (!Enum.TryParse<DayOfWeek>(value, ignoreCase: true, out var day))
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} is not a valid day of week.");
        }

        return day;
    }

    private static void AddWorktimeSettings(RegionSetupWorktime? worktime, List<(string Type, string Value)> settings)
    {
        if (worktime == null)
        {
            return;
        }

        AddNumber(settings, SettingKeys.MaximumHours, worktime.MaximumHours);
        AddNumber(settings, SettingKeys.MinimumHours, worktime.MinimumHours);
        AddNumber(settings, SettingKeys.FullTime, worktime.FullTime);
        AddNumber(settings, SettingKeys.GuaranteedHours, worktime.GuaranteedHours);
        AddNumber(settings, SettingKeys.DefaultWorkingHours, worktime.DefaultWorkingHours);
        AddNumber(settings, SettingKeys.OvertimeThreshold, worktime.OvertimeThreshold);
        AddNumber(settings, SettingKeys.VacationDaysPerYear, worktime.VacationDaysPerYear);
        AddNumber(settings, SettingKeys.SchedulingMaxDailyHours, worktime.MaxDailyHours);
        AddNumber(settings, SettingKeys.SchedulingMaxWeeklyHours, worktime.MaxWeeklyHours);
        AddNumber(settings, SettingKeys.SchedulingMaxConsecutiveDays, worktime.MaxConsecutiveDays);
        AddNumber(settings, SettingKeys.SchedulingMinRestDays, worktime.MinRestDays);
        AddNumber(settings, SettingKeys.SchedulingMinPauseHours, worktime.MinPauseHours);
    }

    private static void AddSurchargeSettings(RegionSetupSurcharges? surcharges, List<(string Type, string Value)> settings)
    {
        if (surcharges == null)
        {
            return;
        }

        AddNumber(settings, SettingKeys.NightRate, surcharges.NightRate);
        AddNumber(settings, SettingKeys.HolidayRate, surcharges.HolidayRate);
        AddNumber(settings, SettingKeys.WE1Rate, surcharges.We1Rate);
        AddNumber(settings, SettingKeys.WE2Rate, surcharges.We2Rate);
        AddNumber(settings, SettingKeys.WE3Rate, surcharges.We3Rate);
    }

    private static void AddExportSettings(RegionSetupExport? export, List<(string Type, string Value)> settings)
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

    private static void AddNumber(List<(string Type, string Value)> settings, string type, decimal? value)
    {
        if (value.HasValue)
        {
            settings.Add((type, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private async Task InstallLanguagesAsync(IReadOnlyList<string> codes)
    {
        foreach (var code in codes)
        {
            var installed = await _languagePluginService.InstallAsync(code);
            if (!installed)
            {
                throw new InvalidRequestException($"Region setup: installation of language plugin '{code}' failed.");
            }
        }
    }

    private async Task<Guid?> ResolveCalendarSelectionIdAsync(RegionSetupCalendarSelection? calendarSelection)
    {
        if (calendarSelection == null)
        {
            return null;
        }

        var country = calendarSelection.Country!.Trim();
        var state = calendarSelection.State?.Trim() ?? string.Empty;
        var countrywideState = country;

        var ids = state.Length > 0
            ? await _calendarSelectionRepository.GetIdsByStateAsync(country, state)
            : new List<Guid>();

        if (ids.Count == 0)
        {
            ids = await _calendarSelectionRepository.GetIdsByStateAsync(country, countrywideState);
        }

        if (ids.Count == 0)
        {
            throw new InvalidRequestException(
                $"Region setup: no calendar selection found for country '{country}' and state '{state}'. " +
                "Install the matching language plugin or create a calendar selection for this region first.");
        }

        if (ids.Count > 1)
        {
            _logger.LogWarning(
                "Region setup: {Count} calendar selections match country '{Country}' and state '{State}', using the first one deterministically",
                ids.Count,
                country,
                state);
        }

        return ids.OrderBy(id => id).First();
    }

    private async Task UpsertSettingAsync(string type, string value)
    {
        var existing = await _settingsRepository.GetSetting(type);
        if (existing != null)
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
        else
        {
            await _settingsRepository.AddSetting(new Domain.Models.Settings.Settings
            {
                Id = Guid.NewGuid(),
                Type = type,
                Value = value
            });
        }
    }

    private static string ComputeSha256Hex(string content)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
