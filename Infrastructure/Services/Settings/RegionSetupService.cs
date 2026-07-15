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
/// the K3/K4 surcharges.stackingMode/overtime fields added later get the same treatment
/// (<see cref="BackfillOvertimeIfPresentAsync"/>); other future field additions to existing sections need
/// the same treatment or a new section of their own.
/// compliance.periodCaps and the top-level industryProfiles map (K20/K5) are ENTITY-import sections, not
/// settings sections: they are never gated by a section marker at all (a per-row
/// ImportSourceKey/ImportContentHash mechanism — see
/// <see cref="Klacks.Api.Application.Services.Setup.EntityImportPlanner"/> — decides insert/update/skip
/// per row instead), so new or changed rows are reconciled on every ApplyAsync call, even on an
/// installation where every settings section is already fully applied. Deploy-order trap: because
/// RegionSetupProfile disallows unmapped JSON members, mounting a region-setup.json that already contains
/// "compliance.periodCaps" or "industryProfiles" while an OLDER binary (without these DTO fields) is still
/// running makes that older binary reject the entire file — deploy the binary carrying the fields before
/// mounting a file that uses them.
/// </summary>
/// <param name="configuration">App configuration providing the RegionSetup:File path</param>
/// <param name="languagePluginService">Installs the requested language plugins</param>
/// <param name="settingsRepository">Reads and upserts rows of the settings table</param>
/// <param name="calendarSelectionRepository">Resolves the global calendar selection by country/state</param>
/// <param name="periodCapRuleRepository">Reads and upserts PeriodCapRule rows for the K20 entity-import path</param>
/// <param name="schedulingRuleImportRepository">Reads and upserts imported SchedulingRule preset rows (K20)</param>
/// <param name="qualificationImportRepository">Reads and upserts imported Qualification catalog rows (K20)</param>
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
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Staffs;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Staffs;

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

    private const string StackingModeHighestWins = SurchargeStackingModeValues.HighestWins;
    private const string StackingModeAdditive = SurchargeStackingModeValues.Additive;
    private const string OvertimeBasisDay = "day";
    private const string OvertimeBasisWeek = "week";
    private const int MaxOvertimeTiers = 3;

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

    private static readonly IReadOnlyList<string> OvertimeTierAfterHoursKeys = new[]
    {
        SettingKeys.OvertimeTier1AfterHours, SettingKeys.OvertimeTier2AfterHours, SettingKeys.OvertimeTier3AfterHours,
    };

    private static readonly IReadOnlyList<string> OvertimeTierRateKeys = new[]
    {
        SettingKeys.OvertimeTier1Rate, SettingKeys.OvertimeTier2Rate, SettingKeys.OvertimeTier3Rate,
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
    private readonly ISchedulingRuleImportRepository _schedulingRuleImportRepository;
    private readonly IQualificationImportRepository _qualificationImportRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegionSetupService> _logger;

    public RegionSetupService(
        IConfiguration configuration,
        ILanguagePluginService languagePluginService,
        ISettingsRepository settingsRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        IPeriodCapRuleRepository periodCapRuleRepository,
        ISchedulingRuleImportRepository schedulingRuleImportRepository,
        IQualificationImportRepository qualificationImportRepository,
        IUnitOfWork unitOfWork,
        ILogger<RegionSetupService> logger)
    {
        _configuration = configuration;
        _languagePluginService = languagePluginService;
        _settingsRepository = settingsRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _periodCapRuleRepository = periodCapRuleRepository;
        _schedulingRuleImportRepository = schedulingRuleImportRepository;
        _qualificationImportRepository = qualificationImportRepository;
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
            await BackfillOvertimeIfPresentAsync(filePath);
        }

        var allSectionsApplied = sectionMarkerExists.Values.All(exists => exists);

        if (allSectionsApplied)
        {
            await ApplyEntityImportsOnFullyAppliedInstallationAsync(filePath);
            return;
        }

        var globalMarker = await _settingsRepository.GetSetting(SettingKeys.RegionSetupApplied);
        var content = await RegionSetupFileReader.ReadContentAsync(filePath);
        var profile = RegionSetupFileReader.Parse(content, filePath);

        var periodCapDesired = BuildPeriodCapEntityDesired(profile.Compliance?.PeriodCaps);
        var (periodCapDecisions, periodCapExistingBySourceKey) = await PlanPeriodCapImportAsync(periodCapDesired);

        var (rulePresetDesired, qualificationDesired) = BuildIndustryProfileDesired(profile.IndustryProfiles);
        var (rulePresetDecisions, rulePresetExistingBySourceKey) = await PlanSchedulingRulePresetImportAsync(rulePresetDesired);
        var (qualificationDecisions, qualificationExistingBySourceKey) = await PlanQualificationImportAsync(qualificationDesired);

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
            ApplySchedulingRulePresetDecisions(rulePresetDecisions, rulePresetExistingBySourceKey);
            ApplyQualificationDecisions(qualificationDecisions, qualificationExistingBySourceKey);
            await _unitOfWork.CompleteAsync();
            return plannedSettings.Count;
        });

        _logger.LogInformation(
            "Region setup applied: region '{Region}', {LanguageCount} language plugin(s) installed, {SettingCount} setting(s) written, {SectionCount} section marker(s) recorded, {PeriodCapCount} period cap row(s), {RulePresetCount} scheduling rule preset(s) and {QualificationCount} qualification catalog row(s) reconciled",
            profile.Region,
            languagesToInstall.Count,
            plannedSettings.Count,
            sectionMarkersToWrite.Count,
            periodCapDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            rulePresetDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            qualificationDecisions.Count(d => d.Action != EntityImportAction.SkipEdited));
    }

    // K20 entity-import path for an installation where every settings section is already fully applied
    // (the whole-file/section-marker mechanism has nothing left to do). compliance.periodCaps and
    // industryProfiles are never gated by a section marker, so they still need reconciling here, but a
    // missing or no-longer-valid profile file must never block startup on a repeat run - same tolerance
    // as the night-window backfill above, just for entity rows instead of settings.
    private async Task ApplyEntityImportsOnFullyAppliedInstallationAsync(string filePath)
    {
        var desired = await TryBuildEntityImportDesiredIfPresentAsync(filePath);
        if (desired == null
            || (desired.PeriodCaps.Count == 0 && desired.RulePresets.Count == 0 && desired.Qualifications.Count == 0))
        {
            _logger.LogInformation("Region setup already applied for all known sections, skipping");
            return;
        }

        var (periodCapDecisions, periodCapExisting) = await PlanPeriodCapImportAsync(desired.PeriodCaps);
        var (rulePresetDecisions, rulePresetExisting) = await PlanSchedulingRulePresetImportAsync(desired.RulePresets);
        var (qualificationDecisions, qualificationExisting) = await PlanQualificationImportAsync(desired.Qualifications);

        var writesNeeded = periodCapDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || rulePresetDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || qualificationDecisions.Any(d => d.Action != EntityImportAction.SkipEdited);
        if (!writesNeeded)
        {
            _logger.LogInformation(
                "Region setup already applied for all known sections; {Count} entity-import row(s) in the file are unchanged or customer-edited, skipping",
                periodCapDecisions.Count + rulePresetDecisions.Count + qualificationDecisions.Count);
            return;
        }

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            ApplyPeriodCapDecisions(periodCapDecisions, periodCapExisting);
            ApplySchedulingRulePresetDecisions(rulePresetDecisions, rulePresetExisting);
            ApplyQualificationDecisions(qualificationDecisions, qualificationExisting);
            await _unitOfWork.CompleteAsync();
            return periodCapDecisions.Count + rulePresetDecisions.Count + qualificationDecisions.Count;
        });

        _logger.LogInformation(
            "Region setup: reconciled {PeriodCapCount} period cap row(s), {RulePresetCount} scheduling rule preset(s) and {QualificationCount} qualification catalog row(s) on an installation with all settings sections already applied",
            periodCapDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            rulePresetDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            qualificationDecisions.Count(d => d.Action != EntityImportAction.SkipEdited));
    }

    private sealed record EntityImportDesiredSets(
        List<EntityImportDesired<PeriodCapRuleImportValues>> PeriodCaps,
        List<EntityImportDesired<SchedulingRulePresetImportValues>> RulePresets,
        List<EntityImportDesired<QualificationCatalogImportValues>> Qualifications);

    private async Task<EntityImportDesiredSets?> TryBuildEntityImportDesiredIfPresentAsync(string filePath)
    {
        try
        {
            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            var periodCaps = BuildPeriodCapEntityDesired(profile.Compliance?.PeriodCaps);
            var (rulePresets, qualifications) = BuildIndustryProfileDesired(profile.IndustryProfiles);
            return new EntityImportDesiredSets(periodCaps, rulePresets, qualifications);
        }
        catch (InvalidRequestException ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: entity-import section skipped due to invalid profile content: {Message}",
                ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: entity-import check skipped due to an error reading or parsing the profile file");
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

    // Same known gap as BackfillNightWindowIfPresentAsync, closed for the K3/K4 fields added to the
    // surcharges section afterward (stackingMode, overtime.*). Best-effort: only writes a setting that is
    // still absent, independent of the section marker; any error is logged and swallowed since an
    // already-configured installation must never fail to start over a profile file it no longer needs.
    // The tier group is treated all-or-nothing (like the fixed-period vs. rolling-average PeriodCap
    // groups) rather than backfilled tier-by-tier — simpler, and a customer who wants to add or change a
    // tier on an already-applied installation can still do so via the settings API directly.
    private async Task BackfillOvertimeIfPresentAsync(string filePath)
    {
        try
        {
            var hasStackingMode = await _settingsRepository.GetSetting(SettingKeys.SurchargeStackingMode) != null;
            var hasAnyTierRate = await HasAnyOvertimeTierRateAsync();
            if (hasStackingMode && hasAnyTierRate)
            {
                return;
            }

            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            var toWrite = new List<(string Type, string Value)>();

            if (!hasStackingMode && !string.IsNullOrWhiteSpace(profile.Surcharges?.StackingMode))
            {
                toWrite.Add((SettingKeys.SurchargeStackingMode, ValidateStackingMode(profile.Surcharges!.StackingMode!, "surcharges.stackingMode")));
            }

            if (!hasAnyTierRate && profile.Surcharges?.Overtime != null)
            {
                var overtime = profile.Surcharges.Overtime;
                if (!string.IsNullOrWhiteSpace(overtime.Basis))
                {
                    toWrite.Add((SettingKeys.OvertimeBasis, ValidateOvertimeBasis(overtime.Basis, "surcharges.overtime.basis")));
                }

                if (!string.IsNullOrWhiteSpace(overtime.RateMode))
                {
                    toWrite.Add((SettingKeys.OvertimeRateMode, ValidateOvertimeRateMode(overtime.RateMode, "surcharges.overtime.rateMode")));
                }

                AddOvertimeTierSettings(overtime.Tiers, toWrite);
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
                "Region setup: backfilled {Count} overtime/stacking-mode setting(s) for an installation with the surcharges section already applied",
                toWrite.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: overtime/stacking-mode backfill skipped due to an error reading or applying the profile file");
        }
    }

    private async Task<bool> HasAnyOvertimeTierRateAsync()
    {
        foreach (var key in OvertimeTierRateKeys)
        {
            if (await _settingsRepository.GetSetting(key) != null)
            {
                return true;
            }
        }

        return false;
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
        AddStackingModeSetting(surcharges.StackingMode, settings);
        AddOvertimeSettings(surcharges.Overtime, settings);
    }

    private static void AddStackingModeSetting(string? stackingMode, List<(string Type, string Value)> settings)
    {
        if (string.IsNullOrWhiteSpace(stackingMode))
        {
            return;
        }

        settings.Add((SettingKeys.SurchargeStackingMode, ValidateStackingMode(stackingMode, "surcharges.stackingMode")));
    }

    private static string ValidateStackingMode(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized != StackingModeHighestWins && normalized != StackingModeAdditive)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'highestWins' or 'additive'.");
        }

        return normalized;
    }

    private static void AddOvertimeSettings(RegionSetupOvertime? overtime, List<(string Type, string Value)> settings)
    {
        if (overtime == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(overtime.Basis))
        {
            settings.Add((SettingKeys.OvertimeBasis, ValidateOvertimeBasis(overtime.Basis, "surcharges.overtime.basis")));
        }

        if (!string.IsNullOrWhiteSpace(overtime.RateMode))
        {
            settings.Add((SettingKeys.OvertimeRateMode, ValidateOvertimeRateMode(overtime.RateMode, "surcharges.overtime.rateMode")));
        }

        AddOvertimeTierSettings(overtime.Tiers, settings);
    }

    private static string ValidateOvertimeBasis(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized != OvertimeBasisDay && normalized != OvertimeBasisWeek)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'day' or 'week'.");
        }

        return normalized;
    }

    private static string ValidateOvertimeRateMode(string value, string fieldName)
    {
        var normalized = value.Trim().ToLowerInvariant();
        if (normalized == RateModeFixedPerShift)
        {
            throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} is not supported — a flat per-shift amount cannot be split across overtime tiers by hours worked. Use 'multiplier' or 'fixedPerHour'.");
        }

        if (normalized != RateModeMultiplier && normalized != RateModeFixedPerHour)
        {
            throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'multiplier' or 'fixedPerHour'.");
        }

        return normalized;
    }

    private static void AddOvertimeTierSettings(List<RegionSetupOvertimeTier>? tiers, List<(string Type, string Value)> settings)
    {
        if (tiers == null)
        {
            return;
        }

        if (tiers.Count > MaxOvertimeTiers)
        {
            throw new InvalidRequestException(
                $"Region setup: surcharges.overtime.tiers supports at most {MaxOvertimeTiers} entries (Overtime1/2/3), found {tiers.Count}.");
        }

        var previousAfterHours = decimal.MinValue;
        for (var i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            if (tier.AfterHours == null || tier.Rate == null)
            {
                throw new InvalidRequestException($"Region setup: surcharges.overtime.tiers[{i}] requires 'afterHours' and 'rate'.");
            }

            if (tier.AfterHours <= previousAfterHours)
            {
                throw new InvalidRequestException($"Region setup: surcharges.overtime.tiers[{i}].afterHours must be strictly ascending across tiers.");
            }

            if (tier.Rate <= 0)
            {
                throw new InvalidRequestException($"Region setup: surcharges.overtime.tiers[{i}].rate must be greater than zero.");
            }

            previousAfterHours = tier.AfterHours.Value;
            settings.Add((OvertimeTierAfterHoursKeys[i], tier.AfterHours.Value.ToString(CultureInfo.InvariantCulture)));
            settings.Add((OvertimeTierRateKeys[i], tier.Rate.Value.ToString(CultureInfo.InvariantCulture)));
        }
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

    private (List<EntityImportDesired<SchedulingRulePresetImportValues>> RulePresets, List<EntityImportDesired<QualificationCatalogImportValues>> Qualifications) BuildIndustryProfileDesired(
        Dictionary<string, RegionSetupIndustryProfile>? industryProfiles)
    {
        var rulePresets = new List<EntityImportDesired<SchedulingRulePresetImportValues>>();
        var qualifications = new List<EntityImportDesired<QualificationCatalogImportValues>>();

        if (industryProfiles == null || industryProfiles.Count == 0)
        {
            return (rulePresets, qualifications);
        }

        var seenRuleKeys = new HashSet<string>();
        var seenQualificationKeys = new HashSet<string>();

        foreach (var (rawIndustry, profile) in industryProfiles)
        {
            var industry = BuildImportSlug(rawIndustry);
            if (industry.Length == 0)
            {
                throw new InvalidRequestException("Region setup: industryProfiles contains an entry with an empty industry key.");
            }

            if (profile == null)
            {
                continue;
            }

            foreach (var preset in profile.SchedulingRulePresets ?? [])
            {
                rulePresets.Add(BuildSchedulingRulePresetDesired(industry, preset, seenRuleKeys));
            }

            foreach (var entry in profile.QualificationCatalog ?? [])
            {
                qualifications.Add(BuildQualificationCatalogDesired(industry, entry, seenQualificationKeys));
            }
        }

        return (rulePresets, qualifications);
    }

    private EntityImportDesired<SchedulingRulePresetImportValues> BuildSchedulingRulePresetDesired(
        string industry, RegionSetupSchedulingRulePreset preset, HashSet<string> seenKeys)
    {
        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            throw new InvalidRequestException(
                $"Region setup: industryProfiles.{industry}.schedulingRulePresets entry requires a non-empty 'name'.");
        }

        var fieldPrefix = $"industryProfiles.{industry}.schedulingRulePresets";
        RequireNonNegative(preset.MaxWorkDays, $"{fieldPrefix}.maxWorkDays");
        RequireNonNegative(preset.MinRestDays, $"{fieldPrefix}.minRestDays");
        RequireNonNegative(preset.MinPauseHours, $"{fieldPrefix}.minPauseHours");
        RequireNonNegative(preset.MaxOptimalGap, $"{fieldPrefix}.maxOptimalGap");
        RequireNonNegative(preset.MaxDailyHours, $"{fieldPrefix}.maxDailyHours");
        RequireNonNegative(preset.MaxWeeklyHours, $"{fieldPrefix}.maxWeeklyHours");
        RequireNonNegative(preset.MaxConsecutiveDays, $"{fieldPrefix}.maxConsecutiveDays");
        RequireNonNegative(preset.DefaultWorkingHours, $"{fieldPrefix}.defaultWorkingHours");
        RequireNonNegative(preset.OvertimeThreshold, $"{fieldPrefix}.overtimeThreshold");
        RequireNonNegative(preset.GuaranteedHours, $"{fieldPrefix}.guaranteedHours");
        RequireNonNegative(preset.MaximumHours, $"{fieldPrefix}.maximumHours");
        RequireNonNegative(preset.MinimumHours, $"{fieldPrefix}.minimumHours");
        RequireNonNegative(preset.FullTimeHours, $"{fieldPrefix}.fullTimeHours");
        RequireNonNegative(preset.VacationDaysPerYear, $"{fieldPrefix}.vacationDaysPerYear");
        RequireNonNegative(preset.NightRate, $"{fieldPrefix}.nightRate");
        RequireNonNegative(preset.HolidayRate, $"{fieldPrefix}.holidayRate");
        RequireNonNegative(preset.We1Rate, $"{fieldPrefix}.we1Rate");
        RequireNonNegative(preset.We2Rate, $"{fieldPrefix}.we2Rate");
        RequireNonNegative(preset.We3Rate, $"{fieldPrefix}.we3Rate");

        if (!string.IsNullOrWhiteSpace(preset.NightStart))
        {
            ValidateTimeOfDay(preset.NightStart!, $"{fieldPrefix}.nightStart");
        }

        if (!string.IsNullOrWhiteSpace(preset.NightEnd))
        {
            ValidateTimeOfDay(preset.NightEnd!, $"{fieldPrefix}.nightEnd");
        }

        var name = preset.Name!.Trim();
        var sourceKey = $"region-setup:industryProfiles:{industry}:rule:{BuildImportSlug(name)}";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: industryProfiles contains more than one scheduling rule preset resolving to key '{sourceKey}'.");
        }

        var values = new SchedulingRulePresetImportValues(
            name,
            preset.MaxWorkDays,
            preset.MinRestDays,
            preset.MinPauseHours,
            preset.MaxOptimalGap,
            preset.MaxDailyHours,
            preset.MaxWeeklyHours,
            preset.MaxConsecutiveDays,
            preset.DefaultWorkingHours,
            preset.OvertimeThreshold,
            preset.GuaranteedHours,
            preset.MaximumHours,
            preset.MinimumHours,
            preset.FullTimeHours,
            preset.VacationDaysPerYear,
            preset.NightRate,
            preset.HolidayRate,
            preset.We1Rate,
            preset.We2Rate,
            preset.We3Rate,
            string.IsNullOrWhiteSpace(preset.NightStart) ? null : preset.NightStart!.Trim(),
            string.IsNullOrWhiteSpace(preset.NightEnd) ? null : preset.NightEnd!.Trim(),
            preset.PerformsShiftWork);

        return new EntityImportDesired<SchedulingRulePresetImportValues>(
            sourceKey,
            ComputeSchedulingRulePresetContentHash(values),
            values);
    }

    private EntityImportDesired<QualificationCatalogImportValues> BuildQualificationCatalogDesired(
        string industry, RegionSetupQualificationCatalogEntry entry, HashSet<string> seenKeys)
    {
        var fieldPrefix = $"industryProfiles.{industry}.qualificationCatalog";
        var names = new Dictionary<string, string>();
        foreach (var (rawLanguage, text) in entry.Name ?? [])
        {
            var language = rawLanguage.Trim().ToLowerInvariant();
            if (!MultiLanguage.CoreLanguages.Contains(language))
            {
                throw new InvalidRequestException(
                    $"Region setup: {fieldPrefix}.name language '{rawLanguage}' is not a core language ({string.Join("/", MultiLanguage.CoreLanguages)}).");
            }

            if (!string.IsNullOrWhiteSpace(text))
            {
                names[language] = text.Trim();
            }
        }

        if (names.Count == 0)
        {
            throw new InvalidRequestException(
                $"Region setup: {fieldPrefix} entry requires a 'name' with at least one non-empty core-language value.");
        }

        var keyName = names.GetValueOrDefault("de")
            ?? names.GetValueOrDefault("en")
            ?? names.GetValueOrDefault("fr")
            ?? names["it"];
        var sourceKey = $"region-setup:industryProfiles:{industry}:qualification:{BuildImportSlug(keyName)}";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: industryProfiles contains more than one qualification catalog entry resolving to key '{sourceKey}'.");
        }

        var values = new QualificationCatalogImportValues(
            names.GetValueOrDefault("de"),
            names.GetValueOrDefault("en"),
            names.GetValueOrDefault("fr"),
            names.GetValueOrDefault("it"),
            entry.IsTimeLimited ?? false,
            MapIndustryToQualificationCategory(industry));

        return new EntityImportDesired<QualificationCatalogImportValues>(
            sourceKey,
            ComputeQualificationContentHash(values.NameDe, values.NameEn, values.NameFr, values.NameIt, values.IsTimeLimited, values.Category),
            values);
    }

    // Used BOTH for the desired hash from the profile file AND for recomputing a stored row's live-value
    // hash - the two calls must format every field identically (see ComputePeriodCapContentHash).
    private static string ComputeSchedulingRulePresetContentHash(SchedulingRulePresetImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.Name,
            FormatInt(values.MaxWorkDays),
            FormatInt(values.MinRestDays),
            FormatDecimal(values.MinPauseHours),
            FormatDecimal(values.MaxOptimalGap),
            FormatDecimal(values.MaxDailyHours),
            FormatDecimal(values.MaxWeeklyHours),
            FormatInt(values.MaxConsecutiveDays),
            FormatDecimal(values.DefaultWorkingHours),
            FormatDecimal(values.OvertimeThreshold),
            FormatDecimal(values.GuaranteedHours),
            FormatDecimal(values.MaximumHours),
            FormatDecimal(values.MinimumHours),
            FormatDecimal(values.FullTimeHours),
            FormatInt(values.VacationDaysPerYear),
            FormatDecimal(values.NightRate),
            FormatDecimal(values.HolidayRate),
            FormatDecimal(values.We1Rate),
            FormatDecimal(values.We2Rate),
            FormatDecimal(values.We3Rate),
            values.NightStart ?? string.Empty,
            values.NightEnd ?? string.Empty,
            FormatBool(values.PerformsShiftWork));
    }

    private static string ComputeQualificationContentHash(
        string? nameDe, string? nameEn, string? nameFr, string? nameIt, bool isTimeLimited, QualificationCategory category)
    {
        return ImportContentHasher.ComputeHash(
            nameDe ?? string.Empty,
            nameEn ?? string.Empty,
            nameFr ?? string.Empty,
            nameIt ?? string.Empty,
            FormatBool(isTimeLimited),
            category.ToString());
    }

    private static string FormatDecimal(decimal? value) =>
        value?.ToString("F4", CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatInt(int? value) =>
        value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;

    private static string FormatBool(bool? value) =>
        value.HasValue ? (value.Value ? "true" : "false") : string.Empty;

    private static void RequireNonNegative(decimal? value, string fieldName)
    {
        if (value < 0)
        {
            throw new InvalidRequestException($"Region setup: {fieldName} must not be negative.");
        }
    }

    private static void RequireNonNegative(int? value, string fieldName)
    {
        if (value < 0)
        {
            throw new InvalidRequestException($"Region setup: {fieldName} must not be negative.");
        }
    }

    // Lowercases and collapses whitespace runs to single dashes. Used for the industry key and for the
    // name part of import source keys - which makes the NAME the identity: renaming a preset in the
    // file creates a new row and leaves the old one behind (documented on the preset DTO).
    private static string BuildImportSlug(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        return string.Join('-', trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static QualificationCategory MapIndustryToQualificationCategory(string industry) => industry switch
    {
        "spitex" => QualificationCategory.Spitex,
        "security" => QualificationCategory.Security,
        "logistics" or "logistik" => QualificationCategory.Logistics,
        "healthcare" or "spitaeler" or "hospitals" => QualificationCategory.Healthcare,
        "gastronomy" or "gastro" => QualificationCategory.Gastronomy,
        "construction" => QualificationCategory.Construction,
        "cleaning" => QualificationCategory.Cleaning,
        "transport" => QualificationCategory.Transport,
        _ => QualificationCategory.Others,
    };

    private async Task<(IReadOnlyList<EntityImportDecision<SchedulingRulePresetImportValues>> Decisions, Dictionary<string, SchedulingRule> ExistingBySourceKey)> PlanSchedulingRulePresetImportAsync(
        IReadOnlyList<EntityImportDesired<SchedulingRulePresetImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _schedulingRuleImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeSchedulingRulePresetContentHash(ToImportValues(r)) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    private static SchedulingRulePresetImportValues ToImportValues(SchedulingRule rule) => new(
        rule.Name,
        rule.MaxWorkDays,
        rule.MinRestDays,
        rule.MinPauseHours,
        rule.MaxOptimalGap,
        rule.MaxDailyHours,
        rule.MaxWeeklyHours,
        rule.MaxConsecutiveDays,
        rule.DefaultWorkingHours,
        rule.OvertimeThreshold,
        rule.GuaranteedHours,
        rule.MaximumHours,
        rule.MinimumHours,
        rule.FullTimeHours,
        rule.VacationDaysPerYear,
        rule.NightRate,
        rule.HolidayRate,
        rule.WE1Rate,
        rule.WE2Rate,
        rule.WE3Rate,
        rule.NightStart,
        rule.NightEnd,
        rule.PerformsShiftWork);

    private void ApplySchedulingRulePresetDecisions(
        IReadOnlyList<EntityImportDecision<SchedulingRulePresetImportValues>> decisions,
        IReadOnlyDictionary<string, SchedulingRule> existingBySourceKey)
    {
        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    var rule = new SchedulingRule { Id = Guid.NewGuid(), ImportSourceKey = decision.SourceKey };
                    CopyPresetValues(decision.Values, rule);
                    rule.ImportContentHash = decision.ContentHash;
                    _schedulingRuleImportRepository.Add(rule);
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    CopyPresetValues(decision.Values, existing);
                    existing.ImportContentHash = decision.ContentHash;
                    _schedulingRuleImportRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: scheduling rule preset '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    private static void CopyPresetValues(SchedulingRulePresetImportValues values, SchedulingRule rule)
    {
        rule.Name = values.Name;
        rule.MaxWorkDays = values.MaxWorkDays;
        rule.MinRestDays = values.MinRestDays;
        rule.MinPauseHours = values.MinPauseHours;
        rule.MaxOptimalGap = values.MaxOptimalGap;
        rule.MaxDailyHours = values.MaxDailyHours;
        rule.MaxWeeklyHours = values.MaxWeeklyHours;
        rule.MaxConsecutiveDays = values.MaxConsecutiveDays;
        rule.DefaultWorkingHours = values.DefaultWorkingHours;
        rule.OvertimeThreshold = values.OvertimeThreshold;
        rule.GuaranteedHours = values.GuaranteedHours;
        rule.MaximumHours = values.MaximumHours;
        rule.MinimumHours = values.MinimumHours;
        rule.FullTimeHours = values.FullTimeHours;
        rule.VacationDaysPerYear = values.VacationDaysPerYear;
        rule.NightRate = values.NightRate;
        rule.HolidayRate = values.HolidayRate;
        rule.WE1Rate = values.We1Rate;
        rule.WE2Rate = values.We2Rate;
        rule.WE3Rate = values.We3Rate;
        rule.NightStart = values.NightStart;
        rule.NightEnd = values.NightEnd;
        rule.PerformsShiftWork = values.PerformsShiftWork;
    }

    private async Task<(IReadOnlyList<EntityImportDecision<QualificationCatalogImportValues>> Decisions, Dictionary<string, Qualification> ExistingBySourceKey)> PlanQualificationImportAsync(
        IReadOnlyList<EntityImportDesired<QualificationCatalogImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _qualificationImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(q => q.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            q => q.ImportSourceKey,
            q => ComputeQualificationContentHash(q.Name.De, q.Name.En, q.Name.Fr, q.Name.It, q.IsTimeLimited, q.Category) == q.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    private void ApplyQualificationDecisions(
        IReadOnlyList<EntityImportDecision<QualificationCatalogImportValues>> decisions,
        IReadOnlyDictionary<string, Qualification> existingBySourceKey)
    {
        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _qualificationImportRepository.Add(new Qualification
                    {
                        Id = Guid.NewGuid(),
                        Name = new MultiLanguage
                        {
                            De = decision.Values.NameDe,
                            En = decision.Values.NameEn,
                            Fr = decision.Values.NameFr,
                            It = decision.Values.NameIt,
                        },
                        IsTimeLimited = decision.Values.IsTimeLimited,
                        Category = decision.Values.Category,
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    existing.Name.De = decision.Values.NameDe;
                    existing.Name.En = decision.Values.NameEn;
                    existing.Name.Fr = decision.Values.NameFr;
                    existing.Name.It = decision.Values.NameIt;
                    existing.IsTimeLimited = decision.Values.IsTimeLimited;
                    existing.Category = decision.Values.Category;
                    existing.ImportContentHash = decision.ContentHash;
                    _qualificationImportRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: qualification catalog entry '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
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
