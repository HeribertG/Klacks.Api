// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// First-run region setup: reads an optional mounted JSON profile, installs language plugins and writes
/// locale, calendar, worktime, surcharge and export settings. Each profile section has its own marker
/// setting so a section is applied exactly once, independently of the others — adding a brand-new section
/// to the schema later still gets applied on an already-configured installation. Any invalid profile
/// content (including an unknown schema version) fails fast before the first write.
/// Known limitation: a field added to an EXISTING section (e.g. surcharges.nightWindow) is not covered by
/// the section marker once that section was already applied on an installation. The night window fields
/// specifically are closed via a dedicated best-effort backfill (<see cref="BackfillNightWindowIfPresentAsync"/>);
/// other future field additions to existing sections need the same treatment or a new section of their own.
/// compliance.periodCaps (K20/K5) is an ENTITY-import sub-section, not a settings sub-section: it is never
/// gated by the compliance section marker at all (a per-row ImportSourceKey/ImportContentHash mechanism —
/// see <see cref="Klacks.Api.Application.Services.Setup.EntityImportPlanner"/> — decides insert/update/skip
/// per row instead), so new or changed cap rows are reconciled on every ApplyAsync call, even on an
/// installation where every settings section is already fully applied. Deploy-order trap: because
/// RegionSetupProfile disallows unmapped JSON members, mounting a region-setup.json that already contains
/// "compliance.periodCaps" while an OLDER binary (without this DTO field) is still running makes that
/// older binary reject the entire file — deploy the binary carrying this field before mounting a file that
/// uses it.
/// </summary>
/// <param name="configuration">App configuration providing the RegionSetup:File path</param>
/// <param name="languagePluginService">Installs the requested language plugins</param>
/// <param name="settingsRepository">Reads and upserts rows of the settings table</param>
/// <param name="calendarSelectionRepository">Resolves the global calendar selection by country/state</param>
/// <param name="periodCapRuleRepository">Reads and upserts PeriodCapRule rows for the K20 entity-import path</param>
/// <param name="unitOfWork">Persists all setting and entity-import writes in one transaction</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Application.Services.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionSetupService : IRegionSetupService
{
    public const string FileConfigKey = RegionSetupFileReader.FileConfigKey;

    private const string ListSeparator = ",";
    private const string SectionAppliedMarkerValue = "true";
    private const string TimeOfDayFormat = "HH:mm";
    private const string EnforcementModeWarn = "warn";
    private const string EnforcementModeBlock = "block";

    private const string SurchargeTypeNight = "night";
    private const string SurchargeTypeHoliday = "holiday";
    private const string SurchargeTypeWeekend1 = "weekend1";
    private const string SurchargeTypeWeekend2 = "weekend2";
    private const string SurchargeTypeWeekend3 = "weekend3";

    private const string RateModeMultiplier = "multiplier";
    private const string RateModeFixedPerHour = "fixedperhour";
    private const string RateModeFixedPerShift = "fixedpershift";

    private enum Section
    {
        Languages,
        Locale,
        Calendar,
        Worktime,
        Surcharges,
        Export,
        Compliance
    }

    private enum SectionAction
    {
        Skip,
        Backfill,
        Apply
    }

    private static readonly IReadOnlyDictionary<Section, string> SectionMarkerKeys = new Dictionary<Section, string>
    {
        [Section.Languages] = SettingKeys.RegionSetupAppliedLanguages,
        [Section.Locale] = SettingKeys.RegionSetupAppliedLocale,
        [Section.Calendar] = SettingKeys.RegionSetupAppliedCalendar,
        [Section.Worktime] = SettingKeys.RegionSetupAppliedWorktime,
        [Section.Surcharges] = SettingKeys.RegionSetupAppliedSurcharges,
        [Section.Export] = SettingKeys.RegionSetupAppliedExport,
        [Section.Compliance] = SettingKeys.RegionSetupAppliedCompliance,
    };

    private static readonly IReadOnlyDictionary<string, string> RateModeSettingKeysByType = new Dictionary<string, string>
    {
        [SurchargeTypeNight] = SettingKeys.SurchargeNightRateMode,
        [SurchargeTypeHoliday] = SettingKeys.SurchargeHolidayRateMode,
        [SurchargeTypeWeekend1] = SettingKeys.SurchargeWE1RateMode,
        [SurchargeTypeWeekend2] = SettingKeys.SurchargeWE2RateMode,
        [SurchargeTypeWeekend3] = SettingKeys.SurchargeWE3RateMode,
    };

    private static readonly IReadOnlyDictionary<string, string> MinimumPerHourSettingKeysByType = new Dictionary<string, string>
    {
        [SurchargeTypeNight] = SettingKeys.SurchargeNightMinimumPerHour,
        [SurchargeTypeHoliday] = SettingKeys.SurchargeHolidayMinimumPerHour,
        [SurchargeTypeWeekend1] = SettingKeys.SurchargeWE1MinimumPerHour,
        [SurchargeTypeWeekend2] = SettingKeys.SurchargeWE2MinimumPerHour,
        [SurchargeTypeWeekend3] = SettingKeys.SurchargeWE3MinimumPerHour,
    };

    // Frozen at the sections that existed before per-section markers were introduced. A section already
    // covered by the legacy whole-file marker counts as applied without rewriting its settings. This list
    // must NEVER grow: a newly introduced section (e.g. "compliance", added below) must go through the
    // normal Apply path on every installation, even one where the legacy whole-file marker is present.
    // Deliberately hard-coded rather than derived from SectionMarkerKeys.Keys, which does grow.
    private static readonly IReadOnlySet<Section> LegacySections = new HashSet<Section>
    {
        Section.Languages,
        Section.Locale,
        Section.Calendar,
        Section.Worktime,
        Section.Surcharges,
        Section.Export,
    };

    private readonly IConfiguration _configuration;
    private readonly ILanguagePluginService _languagePluginService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IPeriodCapRuleRepository _periodCapRuleRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegionSetupService> _logger;

    public RegionSetupService(
        IConfiguration configuration,
        ILanguagePluginService languagePluginService,
        ISettingsRepository settingsRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        IPeriodCapRuleRepository periodCapRuleRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegionSetupService> logger)
    {
        _configuration = configuration;
        _languagePluginService = languagePluginService;
        _settingsRepository = settingsRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _periodCapRuleRepository = periodCapRuleRepository;
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

        var sectionMarkerExists = await LoadSectionMarkerExistenceAsync();

        if (sectionMarkerExists[Section.Surcharges])
        {
            await BackfillNightWindowIfPresentAsync(filePath);
        }

        var allSectionsApplied = sectionMarkerExists.Values.All(exists => exists);

        if (allSectionsApplied)
        {
            await ApplyPeriodCapEntityImportOnFullyAppliedInstallationAsync(filePath);
            return;
        }

        var globalMarker = await _settingsRepository.GetSetting(SettingKeys.RegionSetupApplied);
        var content = await RegionSetupFileReader.ReadContentAsync(filePath);
        var profile = RegionSetupFileReader.Parse(content, filePath);

        var periodCapDesired = BuildPeriodCapEntityDesired(profile.Compliance?.PeriodCaps);
        var (periodCapDecisions, periodCapExistingBySourceKey) = await PlanPeriodCapImportAsync(periodCapDesired);

        var actions = DetermineSectionActions(profile, sectionMarkerExists, globalMarker != null);
        var (languagesToInstall, plannedSettings, sectionMarkersToWrite) = BuildPlan(profile, actions);

        await InstallLanguagesAsync(languagesToInstall);

        if (actions[Section.Locale] == SectionAction.Apply)
        {
            var calendarSelectionId = await ResolveCalendarSelectionIdAsync(profile.Locale?.CalendarSelection);
            if (calendarSelectionId.HasValue)
            {
                plannedSettings.Add((SettingKeys.GlobalCalendarSelectionId, calendarSelectionId.Value.ToString()));
            }
        }

        var markerValue = ComputeSha256Hex(content);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (type, value) in plannedSettings)
            {
                await UpsertSettingAsync(type, value);
            }

            foreach (var markerKey in sectionMarkersToWrite)
            {
                await UpsertSettingAsync(markerKey, SectionAppliedMarkerValue);
            }

            await UpsertSettingAsync(SettingKeys.RegionSetupApplied, markerValue);
            ApplyPeriodCapDecisions(periodCapDecisions, periodCapExistingBySourceKey);
            await _unitOfWork.CompleteAsync();
            return plannedSettings.Count;
        });

        _logger.LogInformation(
            "Region setup applied: region '{Region}', {LanguageCount} language plugin(s) installed, {SettingCount} setting(s) written, {SectionCount} section marker(s) recorded, {PeriodCapCount} period cap row(s) reconciled",
            profile.Region,
            languagesToInstall.Count,
            plannedSettings.Count,
            sectionMarkersToWrite.Count,
            periodCapDecisions.Count(d => d.Action != EntityImportAction.SkipEdited));
    }

    // K20 entity-import path for an installation where every settings section is already fully applied
    // (the whole-file/section-marker mechanism has nothing left to do). compliance.periodCaps is never
    // gated by the compliance section marker, so it still needs reconciling here, but a missing or
    // no-longer-valid profile file must never block startup on a repeat run - same tolerance as the
    // night-window backfill above, just for entity rows instead of settings.
    private async Task ApplyPeriodCapEntityImportOnFullyAppliedInstallationAsync(string filePath)
    {
        var desired = await TryBuildPeriodCapDesiredIfPresentAsync(filePath);
        if (desired == null || desired.Count == 0)
        {
            _logger.LogInformation("Region setup already applied for all known sections, skipping");
            return;
        }

        var (decisions, existingBySourceKey) = await PlanPeriodCapImportAsync(desired);
        var writesNeeded = decisions.Any(d => d.Action != EntityImportAction.SkipEdited);
        if (!writesNeeded)
        {
            _logger.LogInformation(
                "Region setup already applied for all known sections; {Count} period cap row(s) in the file are unchanged or customer-edited, skipping",
                decisions.Count);
            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ApplyPeriodCapDecisions(decisions, existingBySourceKey);
            await _unitOfWork.CompleteAsync();
            return decisions.Count;
        });

        _logger.LogInformation(
            "Region setup: reconciled {Count} period cap row(s) on an installation with all settings sections already applied",
            decisions.Count(d => d.Action != EntityImportAction.SkipEdited));
    }

    private async Task<List<EntityImportDesired<PeriodCapRuleImportValues>>?> TryBuildPeriodCapDesiredIfPresentAsync(string filePath)
    {
        try
        {
            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            return BuildPeriodCapEntityDesired(profile.Compliance?.PeriodCaps);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: period cap entity-import check skipped due to an error reading or parsing the profile file");
            return null;
        }
    }

    private async Task<Dictionary<Section, bool>> LoadSectionMarkerExistenceAsync()
    {
        var result = new Dictionary<Section, bool>();
        foreach (var (section, markerKey) in SectionMarkerKeys)
        {
            result[section] = await _settingsRepository.GetSetting(markerKey) != null;
        }

        return result;
    }

    // Known gap of the per-section marker model (see class summary): once REGION_SETUP_APPLIED_SURCHARGES
    // exists, the surcharges section is never re-applied, so a field added to that section later (like
    // nightWindow) never reaches an installation that already ran setup. This best-effort pass closes that
    // gap for exactly this field: it writes SURCHARGE_NIGHT_START/END only when both the DB value is still
    // absent and the file provides it, independent of the section marker. Any failure (missing file,
    // invalid JSON, invalid time format) is logged and swallowed rather than propagated, because an
    // already-configured installation must never fail to start over a profile file it no longer needs.
    private async Task BackfillNightWindowIfPresentAsync(string filePath)
    {
        try
        {
            var hasStart = await _settingsRepository.GetSetting(SettingKeys.SurchargeNightStart) != null;
            var hasEnd = await _settingsRepository.GetSetting(SettingKeys.SurchargeNightEnd) != null;
            if (hasStart && hasEnd)
            {
                return;
            }

            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            var nightWindow = profile.Surcharges?.NightWindow;
            if (nightWindow == null)
            {
                return;
            }

            var toWrite = new List<(string Type, string Value)>();
            if (!hasStart && !string.IsNullOrWhiteSpace(nightWindow.Start))
            {
                ValidateTimeOfDay(nightWindow.Start, "surcharges.nightWindow.start");
                toWrite.Add((SettingKeys.SurchargeNightStart, nightWindow.Start.Trim()));
            }

            if (!hasEnd && !string.IsNullOrWhiteSpace(nightWindow.End))
            {
                ValidateTimeOfDay(nightWindow.End, "surcharges.nightWindow.end");
                toWrite.Add((SettingKeys.SurchargeNightEnd, nightWindow.End.Trim()));
            }

            if (toWrite.Count == 0)
            {
                return;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var (type, value) in toWrite)
                {
                    await UpsertSettingAsync(type, value);
                }

                await _unitOfWork.CompleteAsync();
                return toWrite.Count;
            });

            _logger.LogInformation(
                "Region setup: backfilled {Count} night window setting(s) for an installation with the surcharges section already applied",
                toWrite.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: night window backfill skipped due to an error reading or applying the profile file");
        }
    }

    private static Dictionary<Section, SectionAction> DetermineSectionActions(
        RegionSetupProfile profile,
        IReadOnlyDictionary<Section, bool> sectionMarkerExists,
        bool globalMarkerPresent)
    {
        var actions = new Dictionary<Section, SectionAction>();
        foreach (var section in SectionMarkerKeys.Keys)
        {
            if (sectionMarkerExists[section])
            {
                actions[section] = SectionAction.Skip;
            }
            else if (globalMarkerPresent && LegacySections.Contains(section))
            {
                actions[section] = SectionAction.Backfill;
            }
            else if (IsSectionPresent(section, profile))
            {
                actions[section] = SectionAction.Apply;
            }
            else
            {
                actions[section] = SectionAction.Skip;
            }
        }

        return actions;
    }

    private static bool IsSectionPresent(Section section, RegionSetupProfile profile) => section switch
    {
        Section.Languages => profile.Languages != null,
        Section.Locale => profile.Locale != null,
        Section.Calendar => profile.Calendar != null,
        Section.Worktime => profile.Worktime != null,
        Section.Surcharges => profile.Surcharges != null,
        Section.Export => profile.Export != null,
        Section.Compliance => profile.Compliance != null,
        _ => throw new ArgumentOutOfRangeException(nameof(section), section, "Unknown region setup section."),
    };

    private (List<string> LanguagesToInstall, List<(string Type, string Value)> Settings, List<string> SectionMarkersToWrite) BuildPlan(
        RegionSetupProfile profile,
        IReadOnlyDictionary<Section, SectionAction> actions)
    {
        var settings = new List<(string Type, string Value)>();
        var sectionMarkersToWrite = new List<string>();
        var languagesToInstall = new List<string>();

        foreach (var (section, action) in actions)
        {
            if (action == SectionAction.Skip)
            {
                continue;
            }

            sectionMarkersToWrite.Add(SectionMarkerKeys[section]);
            if (action == SectionAction.Backfill)
            {
                continue;
            }

            switch (section)
            {
                case Section.Languages:
                    languagesToInstall = ValidateLanguages(profile.Languages);
                    AddDefaultLanguageSetting(profile.Languages, languagesToInstall, settings);
                    break;
                case Section.Locale:
                    AddLocaleSettings(profile.Locale, settings);
                    break;
                case Section.Calendar:
                    AddCalendarSettings(profile.Calendar, settings);
                    break;
                case Section.Worktime:
                    AddWorktimeSettings(profile.Worktime, settings);
                    break;
                case Section.Surcharges:
                    AddSurchargeSettings(profile.Surcharges, settings);
                    break;
                case Section.Export:
                    AddExportSettings(profile.Export, settings);
                    break;
                case Section.Compliance:
                    AddComplianceSettings(profile.Compliance, settings);
                    break;
            }
        }

        return (languagesToInstall, settings, sectionMarkersToWrite);
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
        AddNightWindowSettings(surcharges.NightWindow, settings);
        AddSurchargeRateModeSettings(surcharges.RateModes, settings);
        AddSurchargeMinimumsPerHourSettings(surcharges.MinimumsPerHour, settings);
    }

    private static void AddSurchargeRateModeSettings(Dictionary<string, string>? rateModes, List<(string Type, string Value)> settings)
    {
        if (rateModes == null)
        {
            return;
        }

        foreach (var (typeName, mode) in rateModes)
        {
            var settingKey = ResolveSurchargeTypeSettingKey(typeName, RateModeSettingKeysByType, "surcharges.rateModes");
            var normalizedMode = ValidateRateMode(mode, $"surcharges.rateModes.{typeName}");
            settings.Add((settingKey, normalizedMode));
        }
    }

    private static void AddSurchargeMinimumsPerHourSettings(Dictionary<string, decimal>? minimumsPerHour, List<(string Type, string Value)> settings)
    {
        if (minimumsPerHour == null)
        {
            return;
        }

        foreach (var (typeName, value) in minimumsPerHour)
        {
            var settingKey = ResolveSurchargeTypeSettingKey(typeName, MinimumPerHourSettingKeysByType, "surcharges.minimumsPerHour");
            settings.Add((settingKey, value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static string ResolveSurchargeTypeSettingKey(string typeName, IReadOnlyDictionary<string, string> settingKeysByType, string fieldName)
    {
        var normalized = typeName.Trim().ToLowerInvariant();
        if (!settingKeysByType.TryGetValue(normalized, out var settingKey))
        {
            throw new InvalidRequestException(
                $"Region setup: unknown surcharge type '{typeName}' in {fieldName}. Valid values: night, holiday, weekend1, weekend2, weekend3.");
        }

        return settingKey;
    }

    private static string ValidateRateMode(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized != RateModeMultiplier && normalized != RateModeFixedPerHour && normalized != RateModeFixedPerShift)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'multiplier', 'fixedPerHour' or 'fixedPerShift'.");
        }

        return normalized;
    }

    private static void AddNightWindowSettings(RegionSetupNightWindow? nightWindow, List<(string Type, string Value)> settings)
    {
        if (nightWindow == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(nightWindow.Start))
        {
            ValidateTimeOfDay(nightWindow.Start, "surcharges.nightWindow.start");
            settings.Add((SettingKeys.SurchargeNightStart, nightWindow.Start.Trim()));
        }

        if (!string.IsNullOrWhiteSpace(nightWindow.End))
        {
            ValidateTimeOfDay(nightWindow.End, "surcharges.nightWindow.end");
            settings.Add((SettingKeys.SurchargeNightEnd, nightWindow.End.Trim()));
        }
    }

    private static void ValidateTimeOfDay(string value, string fieldName)
    {
        if (!TimeOnly.TryParseExact(value.Trim(), TimeOfDayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} is not a valid HH:mm time.");
        }
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

    private static void AddInt(List<(string Type, string Value)> settings, string type, int? value)
    {
        if (value.HasValue)
        {
            settings.Add((type, value.Value.ToString(CultureInfo.InvariantCulture)));
        }
    }

    private static void AddBool(List<(string Type, string Value)> settings, string type, bool? value)
    {
        if (value.HasValue)
        {
            settings.Add((type, value.Value ? "true" : "false"));
        }
    }

    private static void AddComplianceSettings(RegionSetupCompliance? compliance, List<(string Type, string Value)> settings)
    {
        if (compliance == null)
        {
            return;
        }

        AddQualificationComplianceSettings(compliance.Qualifications, settings);
        AddEnforcementSettings(compliance.Enforcement, settings);
        AddRosterPublicationSettings(compliance.RosterPublication, settings);
    }

    private static void AddQualificationComplianceSettings(RegionSetupQualifications? qualifications, List<(string Type, string Value)> settings)
    {
        if (qualifications == null)
        {
            return;
        }

        AddBool(settings, SettingKeys.QualificationExpiredMandatoryBlocks, qualifications.ExpiredMandatoryBlocks);
        AddInt(settings, SettingKeys.QualificationExpiryWarningDays, qualifications.ExpiryWarningDays);
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

        AddBool(settings, SettingKeys.ComplianceEnforcementAllowSupervisorOverride, enforcement.AllowSupervisorOverride);

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
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized != EnforcementModeWarn && normalized != EnforcementModeBlock)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'warn' or 'block'.");
        }

        return normalized;
    }

    private static void AddRosterPublicationSettings(RegionSetupRosterPublication? rosterPublication, List<(string Type, string Value)> settings)
    {
        if (rosterPublication == null)
        {
            return;
        }

        AddInt(settings, SettingKeys.ComplianceRosterPublicationMinLeadDays, rosterPublication.MinLeadDays);
        AddBool(settings, SettingKeys.ComplianceRosterPublicationCountWorkdaysOnly, rosterPublication.CountWorkdaysOnly);
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

    private static List<EntityImportDesired<PeriodCapRuleImportValues>> BuildPeriodCapEntityDesired(List<RegionSetupPeriodCap>? periodCaps)
    {
        if (periodCaps == null || periodCaps.Count == 0)
        {
            return [];
        }

        var seenKeys = new HashSet<string>();
        var desired = new List<EntityImportDesired<PeriodCapRuleImportValues>>();

        foreach (var cap in periodCaps)
        {
            var hasFixedPeriodFields = cap.Period != null || cap.Scope != null || cap.CapHours != null;
            var hasRollingAverageFields = cap.WindowWeeks != null || cap.MaxAverageWeeklyHours != null;

            if (hasFixedPeriodFields && hasRollingAverageFields)
            {
                throw new InvalidRequestException(
                    "Region setup: compliance.periodCaps entry mixes fixed-period fields ('period'/'scope'/'capHours') with rolling-average fields ('windowWeeks'/'maxAverageWeeklyHours'); each entry must use exactly one mode.");
            }

            if (cap.WarnAtPercent is < 1 or > 100)
            {
                throw new InvalidRequestException("Region setup: compliance.periodCaps.warnAtPercent must be between 1 and 100.");
            }

            if (hasRollingAverageFields)
            {
                desired.Add(BuildRollingAverageDesired(cap, seenKeys));
            }
            else if (hasFixedPeriodFields)
            {
                desired.Add(BuildFixedPeriodCapDesired(cap, seenKeys));
            }
            else
            {
                throw new InvalidRequestException(
                    "Region setup: compliance.periodCaps entry must specify either fixed-period fields ('period'/'scope'/'capHours') or rolling-average fields ('windowWeeks'/'maxAverageWeeklyHours').");
            }
        }

        return desired;
    }

    private static EntityImportDesired<PeriodCapRuleImportValues> BuildFixedPeriodCapDesired(RegionSetupPeriodCap cap, HashSet<string> seenKeys)
    {
        if (cap.Period == null || cap.Scope == null || cap.CapHours == null)
        {
            throw new InvalidRequestException(
                "Region setup: compliance.periodCaps fixed-period entry requires 'period', 'scope' and 'capHours'.");
        }

        var period = ValidatePeriodCapPeriod(cap.Period, "compliance.periodCaps.period");
        var scope = ValidatePeriodCapScope(cap.Scope, "compliance.periodCaps.scope");

        if (cap.CapHours <= 0)
        {
            throw new InvalidRequestException("Region setup: compliance.periodCaps.capHours must be greater than zero.");
        }

        var sourceKey = $"region-setup:compliance.periodCaps:{period.ToString().ToLowerInvariant()}:{scope.ToString().ToLowerInvariant()}";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: compliance.periodCaps contains more than one entry for period '{cap.Period}' and scope '{cap.Scope}'.");
        }

        var contentHash = ComputePeriodCapContentHash(period, scope, cap.CapHours.Value, cap.WarnAtPercent, null, null);

        return new EntityImportDesired<PeriodCapRuleImportValues>(
            sourceKey,
            contentHash,
            new PeriodCapRuleImportValues(period, scope, cap.CapHours.Value, cap.WarnAtPercent, null, null));
    }

    private static EntityImportDesired<PeriodCapRuleImportValues> BuildRollingAverageDesired(RegionSetupPeriodCap cap, HashSet<string> seenKeys)
    {
        if (cap.WindowWeeks == null || cap.MaxAverageWeeklyHours == null)
        {
            throw new InvalidRequestException(
                "Region setup: compliance.periodCaps rolling-average entry requires 'windowWeeks' and 'maxAverageWeeklyHours'.");
        }

        if (cap.WindowWeeks <= 0)
        {
            throw new InvalidRequestException("Region setup: compliance.periodCaps.windowWeeks must be greater than zero.");
        }

        if (cap.MaxAverageWeeklyHours <= 0)
        {
            throw new InvalidRequestException("Region setup: compliance.periodCaps.maxAverageWeeklyHours must be greater than zero.");
        }

        var sourceKey = $"region-setup:compliance.periodCaps:rolling:{cap.WindowWeeks.Value}w";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: compliance.periodCaps contains more than one rolling-average entry for windowWeeks '{cap.WindowWeeks}'.");
        }

        var contentHash = ComputePeriodCapContentHash(
            PeriodCapPeriod.Month,
            PeriodCapScope.TotalHours,
            0m,
            cap.WarnAtPercent,
            cap.WindowWeeks,
            cap.MaxAverageWeeklyHours);

        return new EntityImportDesired<PeriodCapRuleImportValues>(
            sourceKey,
            contentHash,
            new PeriodCapRuleImportValues(PeriodCapPeriod.Month, PeriodCapScope.TotalHours, 0m, cap.WarnAtPercent, cap.WindowWeeks, cap.MaxAverageWeeklyHours));
    }

    // Used BOTH when building the desired hash from the profile file (above) AND when recomputing a
    // stored row's live-value hash (PlanPeriodCapImportAsync) - the two calls must format every field
    // identically, or an unedited row would falsely look edited (or vice versa) on every re-apply. F4
    // pins the decimal scale so a value read back from Postgres (which can normalize trailing zeros
    // differently to how System.Text.Json deserialized it) still hashes to the same string. Period/Scope/
    // CapHours are unused placeholder defaults for a rolling-average row and RollingWindowWeeks/
    // MaxAverageWeeklyHours are unused (null) for a fixed-period row - hashing every field regardless of
    // mode keeps the two call sites trivially in sync and still changes the hash if a row is ever
    // migrated between modes.
    private static string ComputePeriodCapContentHash(
        PeriodCapPeriod period,
        PeriodCapScope scope,
        decimal capHours,
        int? warnAtPercent,
        int? rollingWindowWeeks,
        decimal? maxAverageWeeklyHours)
    {
        return ImportContentHasher.ComputeHash(
            period.ToString(),
            scope.ToString(),
            capHours.ToString("F4", CultureInfo.InvariantCulture),
            warnAtPercent?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            rollingWindowWeeks?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            maxAverageWeeklyHours?.ToString("F4", CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static PeriodCapPeriod ValidatePeriodCapPeriod(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "month" => PeriodCapPeriod.Month,
            "quarter" => PeriodCapPeriod.Quarter,
            "year" => PeriodCapPeriod.Year,
            "customweeks" => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} (customWeeks) is not yet supported by region-setup import; use 'month', 'quarter' or 'year'."),
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'month', 'quarter' or 'year'."),
        };
    }

    private static PeriodCapScope ValidatePeriodCapScope(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "totalhours" => PeriodCapScope.TotalHours,
            "overtimehours" => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} (overtimeHours) is not yet supported by region-setup import; only 'totalHours' in this stage."),
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'totalHours'."),
        };
    }

    private async Task<(IReadOnlyList<EntityImportDecision<PeriodCapRuleImportValues>> Decisions, Dictionary<string, PeriodCapRule> ExistingBySourceKey)> PlanPeriodCapImportAsync(
        IReadOnlyList<EntityImportDesired<PeriodCapRuleImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _periodCapRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        // "Unedited" means the row's CURRENT live values still hash to what was recorded at the last
        // import - NOT whether the file's new desired value happens to equal the old one. Comparing
        // against the file's new hash instead would make it impossible to ever apply a legitimate value
        // change from the file (every changed value would look "edited" even though the customer never
        // touched the row).
        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputePeriodCapContentHash(r.Period, r.Scope, r.CapHours, r.WarnAtPercent, r.RollingWindowWeeks, r.MaxAverageWeeklyHours) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    private void ApplyPeriodCapDecisions(
        IReadOnlyList<EntityImportDecision<PeriodCapRuleImportValues>> decisions,
        IReadOnlyDictionary<string, PeriodCapRule> existingBySourceKey)
    {
        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _periodCapRuleRepository.Add(new PeriodCapRule
                    {
                        Id = Guid.NewGuid(),
                        Period = decision.Values.Period,
                        Scope = decision.Values.Scope,
                        CapHours = decision.Values.CapHours,
                        WarnAtPercent = decision.Values.WarnAtPercent,
                        RollingWindowWeeks = decision.Values.RollingWindowWeeks,
                        MaxAverageWeeklyHours = decision.Values.MaxAverageWeeklyHours,
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    existing.Period = decision.Values.Period;
                    existing.Scope = decision.Values.Scope;
                    existing.CapHours = decision.Values.CapHours;
                    existing.WarnAtPercent = decision.Values.WarnAtPercent;
                    existing.RollingWindowWeeks = decision.Values.RollingWindowWeeks;
                    existing.MaxAverageWeeklyHours = decision.Values.MaxAverageWeeklyHours;
                    existing.ImportContentHash = decision.ContentHash;
                    _periodCapRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: period cap '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
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
