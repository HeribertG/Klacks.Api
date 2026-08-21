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
/// The activeIndustries list is a settings section of its own (marker REGION_SETUP_APPLIED_INDUSTRIES,
/// applied exactly once, validated fail-fast against the industryProfiles keys of the same file); the
/// package identity block (package.country/version) is deliberately NEITHER marker-gated nor
/// once-only — it is re-applied on every run, including the fully-applied fast path, so a newer
/// marketplace package version updates REGION_PACKAGE_COUNTRY/REGION_PACKAGE_VERSION.
/// </summary>
/// <param name="configuration">App configuration providing the RegionSetup:File path</param>
/// <param name="languagePluginService">Installs the requested language plugins</param>
/// <param name="settingsRepository">Reads and upserts rows of the settings table</param>
/// <param name="calendarSelectionRepository">Resolves the global calendar selection by country/state</param>
/// <param name="periodCapRuleRepository">Reads and upserts PeriodCapRule rows for the K20 entity-import path</param>
/// <param name="schedulingRuleImportRepository">Reads and upserts imported SchedulingRule preset rows (K20)</param>
/// <param name="schedulingRuleRateRevisionImportRepository">Reads and upserts imported dated rate-revision rows of scheduling rule presets (K20)</param>
/// <param name="qualificationImportRepository">Reads and upserts imported Qualification catalog rows (K20)</param>
/// <param name="macroScriptValidator">Compiles and probe-executes imported macro scripts inside a hard timeout</param>
/// <param name="unitOfWork">Persists all setting and entity-import writes in one transaction</param>
/// <param name="eventDispatcher">Raises a post-commit SchedulingRuleRateRevisionsImportedEvent when rate revisions or rule surcharge fields actually changed, and a post-commit SurchargeSettingsChangedEvent when surcharge-relevant global settings actually changed (never on a no-op import)</param>
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
using Klacks.Api.Domain.Events;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Macros;
using Klacks.Api.Domain.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces.Staffs;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionSetupService : IRegionSetupService, IRegionEntityImportService
{
    public const string FileConfigKey = RegionSetupFileReader.FileConfigKey;

    private const string ListSeparator = ",";
    private const string SectionAppliedMarkerValue = "true";
    private const string TimeOfDayFormat = "HH:mm";
    private const string RateRevisionDateFormat = "yyyy-MM-dd";
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
    private const int MinCustomPeriodWeeks = 1;
    private const int MaxCustomPeriodWeeks = 104;
    private const int PackageCountryCodeLength = 2;

    private enum Section
    {
        Languages,
        Locale,
        Calendar,
        Worktime,
        Surcharges,
        Export,
        Compliance,
        Industries
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
        [Section.Industries] = SettingKeys.RegionSetupAppliedIndustries,
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
    private readonly IRestDayRotationRuleRepository _restDayRotationRuleRepository;
    private readonly ICounterRuleRepository _counterRuleRepository;
    private readonly IRestrictedTimeWindowRuleRepository _restrictedTimeWindowRuleRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly ISchedulingRuleImportRepository _schedulingRuleImportRepository;
    private readonly ISchedulingRuleRateRevisionImportRepository _schedulingRuleRateRevisionImportRepository;
    private readonly IQualificationImportRepository _qualificationImportRepository;
    private readonly IMacroImportRepository _macroImportRepository;
    private readonly IMacroScriptValidator _macroScriptValidator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<RegionSetupService> _logger;

    public RegionSetupService(
        IConfiguration configuration,
        ILanguagePluginService languagePluginService,
        ISettingsRepository settingsRepository,
        ICalendarSelectionRepository calendarSelectionRepository,
        IPeriodCapRuleRepository periodCapRuleRepository,
        IRestDayRotationRuleRepository restDayRotationRuleRepository,
        ICounterRuleRepository counterRuleRepository,
        IRestrictedTimeWindowRuleRepository restrictedTimeWindowRuleRepository,
        IGroupRepository groupRepository,
        ISchedulingRuleImportRepository schedulingRuleImportRepository,
        ISchedulingRuleRateRevisionImportRepository schedulingRuleRateRevisionImportRepository,
        IQualificationImportRepository qualificationImportRepository,
        IMacroImportRepository macroImportRepository,
        IMacroScriptValidator macroScriptValidator,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<RegionSetupService> logger)
    {
        _configuration = configuration;
        _languagePluginService = languagePluginService;
        _settingsRepository = settingsRepository;
        _calendarSelectionRepository = calendarSelectionRepository;
        _periodCapRuleRepository = periodCapRuleRepository;
        _restDayRotationRuleRepository = restDayRotationRuleRepository;
        _counterRuleRepository = counterRuleRepository;
        _restrictedTimeWindowRuleRepository = restrictedTimeWindowRuleRepository;
        _groupRepository = groupRepository;
        _schedulingRuleImportRepository = schedulingRuleImportRepository;
        _schedulingRuleRateRevisionImportRepository = schedulingRuleRateRevisionImportRepository;
        _qualificationImportRepository = qualificationImportRepository;
        _macroImportRepository = macroImportRepository;
        _macroScriptValidator = macroScriptValidator;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
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

        if (sectionMarkerExists[Section.Compliance])
        {
            await BackfillCompensatoryRestIfPresentAsync(filePath);
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

        var industryDesired = BuildIndustryProfileDesired(profile.IndustryProfiles);

        var periodCapDesired = BuildPeriodCapEntityDesired(
            profile.Compliance?.PeriodCaps, "compliance.periodCaps", "region-setup:compliance.periodCaps");
        periodCapDesired.AddRange(industryDesired.BoundPeriodCaps);
        var (periodCapDecisions, periodCapExistingBySourceKey) = await PlanPeriodCapImportAsync(periodCapDesired);

        var restDayRotationDesired = BuildRestDayRotationDesired(
            profile.Compliance?.RestDayRotations, "compliance.restDayRotations", "region-setup:compliance.restDayRotations");
        restDayRotationDesired.AddRange(industryDesired.BoundRestDayRotations);
        var (restDayRotationDecisions, restDayRotationExistingBySourceKey) = await PlanRestDayRotationImportAsync(restDayRotationDesired);

        var counterRuleDesired = BuildCounterRuleDesired(
            profile.Compliance?.CounterRules, "compliance.counterRules", "region-setup:compliance.counterRules");
        counterRuleDesired.AddRange(industryDesired.BoundCounterRules);
        var (counterRuleDecisions, counterRuleExistingBySourceKey) = await PlanCounterRuleImportAsync(counterRuleDesired);

        var restrictedTimeWindowDesired = BuildRestrictedTimeWindowDesired(
            profile.Compliance?.RestrictedTimeWindows, "compliance.restrictedTimeWindows", "region-setup:compliance.restrictedTimeWindows");
        var (restrictedTimeWindowDecisions, restrictedTimeWindowExistingBySourceKey) = await PlanRestrictedTimeWindowImportAsync(restrictedTimeWindowDesired);
        await WarnOnMissingGroupTagsAsync(restrictedTimeWindowDesired);

        var (rulePresetDecisions, rulePresetExistingBySourceKey) = await PlanSchedulingRulePresetImportAsync(industryDesired.RulePresets);
        var (rateRevisionDecisions, rateRevisionExistingBySourceKey) = await PlanRateRevisionImportAsync(industryDesired.RateRevisions);
        var (qualificationDecisions, qualificationExistingBySourceKey) = await PlanQualificationImportAsync(industryDesired.Qualifications);

        var macroDesired = BuildMacroDesired(profile.Macros);
        var (macroDecisions, macroExistingBySourceKey, macroDemotions) = await PlanMacroImportAsync(macroDesired);

        var actions = DetermineSectionActions(profile, sectionMarkerExists, globalMarker != null);
        var (languagesToInstall, plannedSettings, sectionMarkersToWrite) = BuildPlan(profile, actions);

        AddPackageIdentitySettings(profile.Package, plannedSettings);

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

        var changedSurchargeSettingKeys = await ComputeChangedSurchargeRelevantKeysAsync(plannedSettings);

        List<RateRevisionChange> rateRevisionChanges = [];
        List<Guid> changedSurchargeRuleIds = [];

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (type, value) in plannedSettings)
            {
                await _settingsRepository.UpsertSettingAsync(type, value);
            }

            foreach (var markerKey in sectionMarkersToWrite)
            {
                await _settingsRepository.UpsertSettingAsync(markerKey, SectionAppliedMarkerValue);
            }

            await _settingsRepository.UpsertSettingAsync(SettingKeys.RegionSetupApplied, markerValue);
            var (ruleIdBySourceKey, presetSurchargeChanges) = ApplySchedulingRulePresetDecisions(rulePresetDecisions, rulePresetExistingBySourceKey);
            changedSurchargeRuleIds = presetSurchargeChanges;
            rateRevisionChanges = ApplyRateRevisionDecisions(rateRevisionDecisions, rateRevisionExistingBySourceKey, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyPeriodCapDecisions(periodCapDecisions, periodCapExistingBySourceKey, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyRestDayRotationDecisions(restDayRotationDecisions, restDayRotationExistingBySourceKey, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyCounterRuleDecisions(counterRuleDecisions, counterRuleExistingBySourceKey, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyRestrictedTimeWindowDecisions(restrictedTimeWindowDecisions, restrictedTimeWindowExistingBySourceKey);
            ApplyQualificationDecisions(qualificationDecisions, qualificationExistingBySourceKey);
            await ApplyMacroDemotionsAsync(macroDemotions);
            ApplyMacroDecisions(macroDecisions, macroExistingBySourceKey);
            await _unitOfWork.CompleteAsync();
            return plannedSettings.Count;
        });

        await DispatchRateRevisionsImportedAsync(rateRevisionChanges, changedSurchargeRuleIds);
        await DispatchSurchargeSettingsChangedAsync(changedSurchargeSettingKeys);

        _logger.LogInformation(
            "Region setup applied: region '{Region}', {LanguageCount} language plugin(s) installed, {SettingCount} setting(s) written, {SectionCount} section marker(s) recorded, {PeriodCapCount} period cap row(s), {RestDayRotationCount} rest-day rotation(s), {RulePresetCount} scheduling rule preset(s) and {QualificationCount} qualification catalog row(s) reconciled",
            profile.Region,
            languagesToInstall.Count,
            plannedSettings.Count,
            sectionMarkersToWrite.Count,
            periodCapDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            restDayRotationDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
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
        await ApplyPackageIdentityIfPresentAsync(filePath);

        var desired = await TryBuildEntityImportDesiredIfPresentAsync(filePath);
        if (desired == null
            || (desired.PeriodCaps.Count == 0 && desired.RestDayRotations.Count == 0 && desired.RulePresets.Count == 0
                && desired.RateRevisions.Count == 0 && desired.Qualifications.Count == 0 && desired.Macros.Count == 0 && desired.CounterRules.Count == 0
                && desired.RestrictedTimeWindows.Count == 0))
        {
            _logger.LogInformation("Region setup already applied for all known sections, skipping");
            return;
        }

        var (periodCapDecisions, periodCapExisting) = await PlanPeriodCapImportAsync(desired.PeriodCaps);
        var (restDayRotationDecisions, restDayRotationExisting) = await PlanRestDayRotationImportAsync(desired.RestDayRotations);
        var (rulePresetDecisions, rulePresetExisting) = await PlanSchedulingRulePresetImportAsync(desired.RulePresets);
        var (rateRevisionDecisions, rateRevisionExisting) = await PlanRateRevisionImportAsync(desired.RateRevisions);
        var (qualificationDecisions, qualificationExisting) = await PlanQualificationImportAsync(desired.Qualifications);
        var (macroDecisions, macroExisting, macroDemotions) = await PlanMacroImportAsync(desired.Macros);
        var (counterRuleDecisions, counterRuleExisting) = await PlanCounterRuleImportAsync(desired.CounterRules);
        var (restrictedTimeWindowDecisions, restrictedTimeWindowExisting) = await PlanRestrictedTimeWindowImportAsync(desired.RestrictedTimeWindows);
        await WarnOnMissingGroupTagsAsync(desired.RestrictedTimeWindows);

        var writesNeeded = periodCapDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || restDayRotationDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || rulePresetDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || rateRevisionDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || qualificationDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || macroDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || counterRuleDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || restrictedTimeWindowDecisions.Any(d => d.Action != EntityImportAction.SkipEdited);
        if (!writesNeeded)
        {
            _logger.LogInformation(
                "Region setup already applied for all known sections; {Count} entity-import row(s) in the file are unchanged or customer-edited, skipping",
                periodCapDecisions.Count + restDayRotationDecisions.Count + rulePresetDecisions.Count + qualificationDecisions.Count + macroDecisions.Count + counterRuleDecisions.Count + restrictedTimeWindowDecisions.Count);
            return;
        }

        List<RateRevisionChange> rateRevisionChanges = [];
        List<Guid> changedSurchargeRuleIds = [];

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var (ruleIdBySourceKey, presetSurchargeChanges) = ApplySchedulingRulePresetDecisions(rulePresetDecisions, rulePresetExisting);
            changedSurchargeRuleIds = presetSurchargeChanges;
            rateRevisionChanges = ApplyRateRevisionDecisions(rateRevisionDecisions, rateRevisionExisting, desired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyPeriodCapDecisions(periodCapDecisions, periodCapExisting, desired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyRestDayRotationDecisions(restDayRotationDecisions, restDayRotationExisting, desired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyCounterRuleDecisions(counterRuleDecisions, counterRuleExisting, desired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyRestrictedTimeWindowDecisions(restrictedTimeWindowDecisions, restrictedTimeWindowExisting);
            ApplyQualificationDecisions(qualificationDecisions, qualificationExisting);
            await ApplyMacroDemotionsAsync(macroDemotions);
            ApplyMacroDecisions(macroDecisions, macroExisting);
            await _unitOfWork.CompleteAsync();
            return periodCapDecisions.Count + restDayRotationDecisions.Count + rulePresetDecisions.Count + qualificationDecisions.Count + macroDecisions.Count;
        });

        await DispatchRateRevisionsImportedAsync(rateRevisionChanges, changedSurchargeRuleIds);

        _logger.LogInformation(
            "Region setup: reconciled {PeriodCapCount} period cap row(s), {RestDayRotationCount} rest-day rotation(s), {RulePresetCount} scheduling rule preset(s), {QualificationCount} qualification catalog row(s), {CounterRuleCount} counter rule(s) and {RestrictedTimeWindowCount} restricted time window(s) on an installation with all settings sections already applied",
            periodCapDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            restDayRotationDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            rulePresetDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            qualificationDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            counterRuleDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            restrictedTimeWindowDecisions.Count(d => d.Action != EntityImportAction.SkipEdited));
    }

    // WP6 marketplace auto-update entry point (IRegionEntityImportService): applies ONLY the parts of a
    // profile that are re-import-safe by design - industryProfiles entity imports (rule presets incl.
    // rate revisions, qualification catalogs, industry-BOUND compliance rows), macros and the package
    // identity settings. Deliberately NOT applied here: every once-only settings section
    // (languages/locale/calendar/worktime/surcharges/export/compliance), the top-level compliance.*
    // entity rows, restricted time windows and the ACTIVE_INDUSTRIES setting (admin sovereignty).
    // Reuses the exact per-row ImportSourceKey/ContentHash/SkipEdited plan+apply machinery of the boot
    // path, so customer-edited rows are never overwritten and a customer-held macro function slot
    // fails the import with an InvalidRequestException before any write.
    public async Task ApplyEntityImportsAsync(RegionSetupProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var industryDesired = BuildIndustryProfileDesired(profile.IndustryProfiles);
        var macroDesired = BuildMacroDesired(profile.Macros);

        var (periodCapDecisions, periodCapExisting) = await PlanPeriodCapImportAsync(industryDesired.BoundPeriodCaps);
        var (restDayRotationDecisions, restDayRotationExisting) = await PlanRestDayRotationImportAsync(industryDesired.BoundRestDayRotations);
        var (counterRuleDecisions, counterRuleExisting) = await PlanCounterRuleImportAsync(industryDesired.BoundCounterRules);
        var (rulePresetDecisions, rulePresetExisting) = await PlanSchedulingRulePresetImportAsync(industryDesired.RulePresets);
        var (rateRevisionDecisions, rateRevisionExisting) = await PlanRateRevisionImportAsync(industryDesired.RateRevisions);
        var (qualificationDecisions, qualificationExisting) = await PlanQualificationImportAsync(industryDesired.Qualifications);
        var (macroDecisions, macroExisting, macroDemotions) = await PlanMacroImportAsync(macroDesired);

        var packageSettings = new List<(string Type, string Value)>();
        AddPackageIdentitySettings(profile.Package, packageSettings);

        var changedPackageSettings = new List<(string Type, string Value)>();
        foreach (var (type, value) in packageSettings)
        {
            var currentValue = (await _settingsRepository.GetSettingNoTracking(type))?.Value;
            if (!string.Equals(currentValue, value, StringComparison.Ordinal))
            {
                changedPackageSettings.Add((type, value));
            }
        }

        var writesNeeded = changedPackageSettings.Count > 0
            || periodCapDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || restDayRotationDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || counterRuleDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || rulePresetDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || rateRevisionDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || qualificationDecisions.Any(d => d.Action != EntityImportAction.SkipEdited)
            || macroDecisions.Any(d => d.Action != EntityImportAction.SkipEdited);
        if (!writesNeeded)
        {
            _logger.LogInformation(
                "Region package entity import: all rows are unchanged or customer-edited and the package identity is current, skipping");
            return;
        }

        List<RateRevisionChange> rateRevisionChanges = [];
        List<Guid> changedSurchargeRuleIds = [];

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (type, value) in changedPackageSettings)
            {
                await _settingsRepository.UpsertSettingAsync(type, value);
            }

            var (ruleIdBySourceKey, presetSurchargeChanges) = ApplySchedulingRulePresetDecisions(rulePresetDecisions, rulePresetExisting);
            changedSurchargeRuleIds = presetSurchargeChanges;
            rateRevisionChanges = ApplyRateRevisionDecisions(rateRevisionDecisions, rateRevisionExisting, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyPeriodCapDecisions(periodCapDecisions, periodCapExisting, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyRestDayRotationDecisions(restDayRotationDecisions, restDayRotationExisting, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyCounterRuleDecisions(counterRuleDecisions, counterRuleExisting, industryDesired.RuleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
            ApplyQualificationDecisions(qualificationDecisions, qualificationExisting);
            await ApplyMacroDemotionsAsync(macroDemotions);
            ApplyMacroDecisions(macroDecisions, macroExisting);
            await _unitOfWork.CompleteAsync();
            return changedPackageSettings.Count + periodCapDecisions.Count + restDayRotationDecisions.Count
                + counterRuleDecisions.Count + rulePresetDecisions.Count + rateRevisionDecisions.Count
                + qualificationDecisions.Count + macroDecisions.Count;
        });

        await DispatchRateRevisionsImportedAsync(rateRevisionChanges, changedSurchargeRuleIds);

        _logger.LogInformation(
            "Region package entity import: reconciled {RulePresetCount} scheduling rule preset(s), {RateRevisionCount} rate revision(s), {QualificationCount} qualification catalog row(s), {PeriodCapCount} bound period cap(s), {RestDayRotationCount} bound rest-day rotation(s), {CounterRuleCount} bound counter rule(s), {MacroCount} macro(s) and {PackageSettingCount} package identity setting(s)",
            rulePresetDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            rateRevisionDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            qualificationDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            periodCapDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            restDayRotationDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            counterRuleDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            macroDecisions.Count(d => d.Action != EntityImportAction.SkipEdited),
            changedPackageSettings.Count);
    }

    private sealed record EntityImportDesiredSets(
        List<EntityImportDesired<PeriodCapRuleImportValues>> PeriodCaps,
        List<EntityImportDesired<RestDayRotationImportValues>> RestDayRotations,
        List<EntityImportDesired<SchedulingRulePresetImportValues>> RulePresets,
        List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> RateRevisions,
        List<EntityImportDesired<QualificationCatalogImportValues>> Qualifications,
        List<EntityImportDesired<MacroImportValues>> Macros,
        List<EntityImportDesired<CounterRuleImportValues>> CounterRules,
        List<EntityImportDesired<RestrictedTimeWindowImportValues>> RestrictedTimeWindows,
        Dictionary<string, string> RuleSourceKeyByBoundEntityKey);

    private async Task<EntityImportDesiredSets?> TryBuildEntityImportDesiredIfPresentAsync(string filePath)
    {
        try
        {
            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            var industryDesired = BuildIndustryProfileDesired(profile.IndustryProfiles);
            var periodCaps = BuildPeriodCapEntityDesired(
                profile.Compliance?.PeriodCaps, "compliance.periodCaps", "region-setup:compliance.periodCaps");
            periodCaps.AddRange(industryDesired.BoundPeriodCaps);
            var restDayRotations = BuildRestDayRotationDesired(
                profile.Compliance?.RestDayRotations, "compliance.restDayRotations", "region-setup:compliance.restDayRotations");
            restDayRotations.AddRange(industryDesired.BoundRestDayRotations);
            var macros = BuildMacroDesired(profile.Macros);
            var counterRules = BuildCounterRuleDesired(
                profile.Compliance?.CounterRules, "compliance.counterRules", "region-setup:compliance.counterRules");
            counterRules.AddRange(industryDesired.BoundCounterRules);
            var restrictedTimeWindows = BuildRestrictedTimeWindowDesired(
                profile.Compliance?.RestrictedTimeWindows, "compliance.restrictedTimeWindows", "region-setup:compliance.restrictedTimeWindows");
            return new EntityImportDesiredSets(
                periodCaps, restDayRotations, industryDesired.RulePresets, industryDesired.RateRevisions, industryDesired.Qualifications, macros, counterRules, restrictedTimeWindows, industryDesired.RuleSourceKeyByBoundEntityKey);
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

            var changedSurchargeSettingKeys = await ComputeChangedSurchargeRelevantKeysAsync(toWrite);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var (type, value) in toWrite)
                {
                    await _settingsRepository.UpsertSettingAsync(type, value);
                }

                await _unitOfWork.CompleteAsync();
                return toWrite.Count;
            });

            await DispatchSurchargeSettingsChangedAsync(changedSurchargeSettingKeys);

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

            var changedSurchargeSettingKeys = await ComputeChangedSurchargeRelevantKeysAsync(toWrite);

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var (type, value) in toWrite)
                {
                    await _settingsRepository.UpsertSettingAsync(type, value);
                }

                await _unitOfWork.CompleteAsync();
                return toWrite.Count;
            });

            await DispatchSurchargeSettingsChangedAsync(changedSurchargeSettingKeys);

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

    // Same known gap as BackfillNightWindowIfPresentAsync/BackfillOvertimeIfPresentAsync, closed for the
    // K12 compensatoryRest fields added to the compliance section afterward. Best-effort: only writes a
    // setting that is still absent, independent of the section marker; any error is logged and swallowed
    // since an already-configured installation must never fail to start over a profile file it no longer
    // needs. Without this backfill an installation that already carries REGION_SETUP_APPLIED_COMPLIANCE
    // would never receive the compensatory-rest settings and the feature would silently stay off.
    private async Task BackfillCompensatoryRestIfPresentAsync(string filePath)
    {
        try
        {
            var hasEnabled = await _settingsRepository.GetSetting(SettingKeys.ComplianceCompensatoryRestEnabled) != null;
            if (hasEnabled)
            {
                return;
            }

            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            var compensatoryRest = profile.Compliance?.CompensatoryRest;
            if (compensatoryRest == null)
            {
                return;
            }

            var toWrite = new List<(string Type, string Value)>();
            AddCompensatoryRestSettings(compensatoryRest, toWrite);
            if (toWrite.Count == 0)
            {
                return;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var (type, value) in toWrite)
                {
                    await _settingsRepository.UpsertSettingAsync(type, value);
                }

                await _unitOfWork.CompleteAsync();
                return toWrite.Count;
            });

            _logger.LogInformation(
                "Region setup: backfilled {Count} compensatory-rest setting(s) for an installation with the compliance section already applied",
                toWrite.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: compensatory-rest backfill skipped due to an error reading or applying the profile file");
        }
    }

    // The package identity must track the mounted file on EVERY start, also on an installation where
    // every settings section is already applied (a marketplace update ships a new version under the
    // same mount path). Best-effort like the backfills: an already-configured installation must never
    // fail to start over a profile file it no longer needs, so any error is logged and swallowed.
    private async Task ApplyPackageIdentityIfPresentAsync(string filePath)
    {
        try
        {
            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            if (profile.Package == null)
            {
                return;
            }

            var toWrite = new List<(string Type, string Value)>();
            AddPackageIdentitySettings(profile.Package, toWrite);

            var changed = new List<(string Type, string Value)>();
            foreach (var (type, value) in toWrite)
            {
                var currentValue = (await _settingsRepository.GetSettingNoTracking(type))?.Value;
                if (!string.Equals(currentValue, value, StringComparison.Ordinal))
                {
                    changed.Add((type, value));
                }
            }

            if (changed.Count == 0)
            {
                return;
            }

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                foreach (var (type, value) in changed)
                {
                    await _settingsRepository.UpsertSettingAsync(type, value);
                }

                await _unitOfWork.CompleteAsync();
                return changed.Count;
            });

            _logger.LogInformation(
                "Region setup: updated {Count} package identity setting(s) on an installation with all settings sections already applied",
                changed.Count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Region setup: package identity update skipped due to an error reading or applying the profile file");
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
        Section.Industries => profile.ActiveIndustries != null,
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
                case Section.Industries:
                    AddActiveIndustriesSettings(profile, settings);
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

            var code = NormalizeLanguageCode(raw);
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

    // Resolves a raw language code to its canonical form: core languages stay lowercase, while a
    // discovered plugin keeps the exact case of its manifest code (e.g. "zh-CN", "zh-TW"), which a
    // blanket ToLowerInvariant would break for the two mixed-case Chinese plugins.
    private string NormalizeLanguageCode(string raw)
    {
        var trimmed = raw.Trim();
        var lower = trimmed.ToLowerInvariant();
        if (LanguagePluginConstants.CoreLanguages.Contains(lower))
        {
            return lower;
        }

        return _languagePluginService.GetPlugin(trimmed)?.Code ?? lower;
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

        var code = NormalizeLanguageCode(languages.Default);
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
        AddCompensatoryRestSettings(compliance.CompensatoryRest, settings);
    }

    private static void AddCompensatoryRestSettings(RegionSetupCompensatoryRest? compensatoryRest, List<(string Type, string Value)> settings)
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

        AddBool(settings, SettingKeys.ComplianceCompensatoryRestEnabled, compensatoryRest.Enabled);
        AddInt(settings, SettingKeys.ComplianceCompensatoryRestDeadlineDays, compensatoryRest.DeadlineDays);
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

    // Fail-fast validation of the industry arming list: every entry must resolve (via the same slug
    // normalization the importer applies to industryProfiles keys) to a key of the industryProfiles map
    // of the SAME file, the list must not be empty ("empty = all" is forbidden; omit the field instead),
    // no entry may equal the reserved IndustrySlugs.Custom marker (shipped content never opts an
    // installation into "custom", that is an admin-only choice made via the settings UI) and
    // industryProfiles must be present at all. Runs inside BuildPlan, ahead of any write.
    private static void AddActiveIndustriesSettings(RegionSetupProfile profile, List<(string Type, string Value)> settings)
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
            .Select(BuildImportSlug)
            .ToHashSet(StringComparer.Ordinal);

        var slugs = new List<string>();
        foreach (var raw in activeIndustries)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                throw new InvalidRequestException("Region setup: activeIndustries contains an empty industry slug.");
            }

            var slug = BuildImportSlug(raw);
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
    private static void AddPackageIdentitySettings(RegionSetupPackage? package, List<(string Type, string Value)> settings)
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

    private static List<EntityImportDesired<PeriodCapRuleImportValues>> BuildPeriodCapEntityDesired(
        List<RegionSetupPeriodCap>? periodCaps, string sectionPath, string sourceKeyPrefix, HashSet<string>? sharedSeenKeys = null)
    {
        if (periodCaps == null || periodCaps.Count == 0)
        {
            return [];
        }

        var seenKeys = sharedSeenKeys ?? new HashSet<string>();
        var desired = new List<EntityImportDesired<PeriodCapRuleImportValues>>();

        foreach (var cap in periodCaps)
        {
            var hasFixedPeriodFields = cap.Period != null || cap.Scope != null || cap.CapHours != null || cap.CustomPeriodWeeks != null;
            var hasRollingAverageFields = cap.WindowWeeks != null || cap.MaxAverageWeeklyHours != null;

            if (hasFixedPeriodFields && hasRollingAverageFields)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry mixes fixed-period fields ('period'/'scope'/'capHours'/'customPeriodWeeks') with rolling-average fields ('windowWeeks'/'maxAverageWeeklyHours'); each entry must use exactly one mode.");
            }

            if (cap.WarnAtPercent is < 1 or > 100)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.warnAtPercent must be between 1 and 100.");
            }

            if (hasRollingAverageFields)
            {
                desired.Add(BuildRollingAverageDesired(cap, seenKeys, sectionPath, sourceKeyPrefix));
            }
            else if (hasFixedPeriodFields)
            {
                desired.Add(BuildFixedPeriodCapDesired(cap, seenKeys, sectionPath, sourceKeyPrefix));
            }
            else
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry must specify either fixed-period fields ('period'/'scope'/'capHours') or rolling-average fields ('windowWeeks'/'maxAverageWeeklyHours').");
            }
        }

        return desired;
    }

    private static EntityImportDesired<PeriodCapRuleImportValues> BuildFixedPeriodCapDesired(
        RegionSetupPeriodCap cap, HashSet<string> seenKeys, string sectionPath, string sourceKeyPrefix)
    {
        if (cap.Period == null || cap.Scope == null || cap.CapHours == null)
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath} fixed-period entry requires 'period', 'scope' and 'capHours'.");
        }

        var period = ValidatePeriodCapPeriod(cap.Period, $"{sectionPath}.period");
        var scope = ValidatePeriodCapScope(cap.Scope, $"{sectionPath}.scope");

        if (cap.CapHours <= 0)
        {
            throw new InvalidRequestException($"Region setup: {sectionPath}.capHours must be greater than zero.");
        }

        if (period == PeriodCapPeriod.CustomWeeks)
        {
            if (cap.CustomPeriodWeeks == null)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry with period 'customWeeks' requires 'customPeriodWeeks'.");
            }

            if (cap.CustomPeriodWeeks is < MinCustomPeriodWeeks or > MaxCustomPeriodWeeks)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath}.customPeriodWeeks must be between {MinCustomPeriodWeeks} and {MaxCustomPeriodWeeks}.");
            }
        }
        else if (cap.CustomPeriodWeeks != null)
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath}.customPeriodWeeks is only allowed when period is 'customWeeks', not '{cap.Period}'.");
        }

        // Month/Quarter/Year keys MUST keep the historic "{prefix}:{period}:{scope}" shape byte-identical -
        // existing installations match their rows via ImportSourceKey, a changed key would orphan every
        // previously imported row and re-insert a duplicate. CustomWeeks (new, no legacy rows) embeds the
        // window width so that two customWeeks caps with different window lengths (e.g. 4w and 52w, NO)
        // can coexist under distinct keys.
        var sourceKey = period == PeriodCapPeriod.CustomWeeks
            ? $"{sourceKeyPrefix}:{period.ToString().ToLowerInvariant()}{cap.CustomPeriodWeeks!.Value}w:{scope.ToString().ToLowerInvariant()}"
            : $"{sourceKeyPrefix}:{period.ToString().ToLowerInvariant()}:{scope.ToString().ToLowerInvariant()}";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                period == PeriodCapPeriod.CustomWeeks
                    ? $"Region setup: {sectionPath} contains more than one entry for period 'customWeeks' with {cap.CustomPeriodWeeks} week(s) and scope '{cap.Scope}'."
                    : $"Region setup: {sectionPath} contains more than one entry for period '{cap.Period}' and scope '{cap.Scope}'.");
        }

        var contentHash = ComputePeriodCapContentHash(period, scope, cap.CapHours.Value, cap.WarnAtPercent, cap.CustomPeriodWeeks, null, null);

        return new EntityImportDesired<PeriodCapRuleImportValues>(
            sourceKey,
            contentHash,
            new PeriodCapRuleImportValues(period, scope, cap.CapHours.Value, cap.WarnAtPercent, cap.CustomPeriodWeeks, null, null));
    }

    private static EntityImportDesired<PeriodCapRuleImportValues> BuildRollingAverageDesired(
        RegionSetupPeriodCap cap, HashSet<string> seenKeys, string sectionPath, string sourceKeyPrefix)
    {
        if (cap.WindowWeeks == null || cap.MaxAverageWeeklyHours == null)
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath} rolling-average entry requires 'windowWeeks' and 'maxAverageWeeklyHours'.");
        }

        if (cap.WindowWeeks <= 0)
        {
            throw new InvalidRequestException($"Region setup: {sectionPath}.windowWeeks must be greater than zero.");
        }

        if (cap.MaxAverageWeeklyHours <= 0)
        {
            throw new InvalidRequestException($"Region setup: {sectionPath}.maxAverageWeeklyHours must be greater than zero.");
        }

        var sourceKey = $"{sourceKeyPrefix}:rolling:{cap.WindowWeeks.Value}w";
        if (!seenKeys.Add(sourceKey))
        {
            throw new InvalidRequestException(
                $"Region setup: {sectionPath} contains more than one rolling-average entry for windowWeeks '{cap.WindowWeeks}'.");
        }

        var contentHash = ComputePeriodCapContentHash(
            PeriodCapPeriod.Month,
            PeriodCapScope.TotalHours,
            0m,
            cap.WarnAtPercent,
            null,
            cap.WindowWeeks,
            cap.MaxAverageWeeklyHours);

        return new EntityImportDesired<PeriodCapRuleImportValues>(
            sourceKey,
            contentHash,
            new PeriodCapRuleImportValues(PeriodCapPeriod.Month, PeriodCapScope.TotalHours, 0m, cap.WarnAtPercent, null, cap.WindowWeeks, cap.MaxAverageWeeklyHours));
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
    // Backward compatibility: customPeriodWeeks contributes to the hash material ONLY when non-null
    // (no extra empty-string field for null, unlike the older nullable fields above). Every row imported
    // before customWeeks existed was hashed from exactly six fields; appending a seventh empty field for
    // null would change the recomputed live hash of ALL those rows, every one would falsely look
    // customer-edited (SkipEdited) and file updates would never be applied again. A null-vs-set ambiguity
    // cannot arise because the period name ("CustomWeeks" vs "Month"/...) is part of the material and
    // customPeriodWeeks is only ever set together with period CustomWeeks (validated on import).
    private static string ComputePeriodCapContentHash(
        PeriodCapPeriod period,
        PeriodCapScope scope,
        decimal capHours,
        int? warnAtPercent,
        int? customPeriodWeeks,
        int? rollingWindowWeeks,
        decimal? maxAverageWeeklyHours)
    {
        var fields = new List<string>
        {
            period.ToString(),
            scope.ToString(),
            capHours.ToString("F4", CultureInfo.InvariantCulture),
            warnAtPercent?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            rollingWindowWeeks?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
            maxAverageWeeklyHours?.ToString("F4", CultureInfo.InvariantCulture) ?? string.Empty,
        };

        if (customPeriodWeeks != null)
        {
            fields.Add(customPeriodWeeks.Value.ToString(CultureInfo.InvariantCulture));
        }

        return ImportContentHasher.ComputeHash(fields.ToArray());
    }

    private static PeriodCapPeriod ValidatePeriodCapPeriod(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "month" => PeriodCapPeriod.Month,
            "quarter" => PeriodCapPeriod.Quarter,
            "year" => PeriodCapPeriod.Year,
            "customweeks" => PeriodCapPeriod.CustomWeeks,
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'month', 'quarter', 'year' or 'customWeeks'."),
        };
    }

    private static PeriodCapScope ValidatePeriodCapScope(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "totalhours" => PeriodCapScope.TotalHours,
            "overtimehours" => PeriodCapScope.OvertimeHours,
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'totalHours' or 'overtimeHours'."),
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
            r => ComputePeriodCapContentHash(r.Period, r.Scope, r.CapHours, r.WarnAtPercent, r.CustomPeriodWeeks, r.RollingWindowWeeks, r.MaxAverageWeeklyHours) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    private void ApplyPeriodCapDecisions(
        IReadOnlyList<EntityImportDecision<PeriodCapRuleImportValues>> decisions,
        IReadOnlyDictionary<string, PeriodCapRule> existingBySourceKey,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
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
                        CustomPeriodWeeks = decision.Values.CustomPeriodWeeks,
                        RollingWindowWeeks = decision.Values.RollingWindowWeeks,
                        MaxAverageWeeklyHours = decision.Values.MaxAverageWeeklyHours,
                        SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey),
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
                    existing.CustomPeriodWeeks = decision.Values.CustomPeriodWeeks;
                    existing.RollingWindowWeeks = decision.Values.RollingWindowWeeks;
                    existing.MaxAverageWeeklyHours = decision.Values.MaxAverageWeeklyHours;
                    existing.SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
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

    private static List<EntityImportDesired<RestDayRotationImportValues>> BuildRestDayRotationDesired(
        List<RegionSetupRestDayRotation>? rotations, string sectionPath, string sourceKeyPrefix, HashSet<string>? sharedSeenKeys = null)
    {
        if (rotations == null || rotations.Count == 0)
        {
            return [];
        }

        var seenKeys = sharedSeenKeys ?? new HashSet<string>();
        var desired = new List<EntityImportDesired<RestDayRotationImportValues>>();

        foreach (var rotation in rotations)
        {
            if (string.IsNullOrWhiteSpace(rotation.DayOfWeek) || rotation.MinFree == null || rotation.WindowWeeks == null)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry requires 'dayOfWeek', 'minFree' and 'windowWeeks'.");
            }

            var dayOfWeek = ParseDayOfWeek(rotation.DayOfWeek.Trim(), $"{sectionPath}.dayOfWeek");

            if (rotation.MinFree <= 0)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.minFree must be greater than zero.");
            }

            if (rotation.WindowWeeks <= 0)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.windowWeeks must be greater than zero.");
            }

            if (rotation.MinFree > rotation.WindowWeeks)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath}.minFree must not exceed windowWeeks - the rule would be impossible to satisfy.");
            }

            var sourceKey = $"{sourceKeyPrefix}:{dayOfWeek.ToString().ToLowerInvariant()}:{rotation.WindowWeeks.Value}w";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} contains more than one entry for day '{rotation.DayOfWeek}' and windowWeeks '{rotation.WindowWeeks}'.");
            }

            var values = new RestDayRotationImportValues(dayOfWeek, rotation.MinFree.Value, rotation.WindowWeeks.Value);
            desired.Add(new EntityImportDesired<RestDayRotationImportValues>(
                sourceKey,
                ComputeRestDayRotationContentHash(values.DayOfWeek, values.MinFree, values.WindowWeeks),
                values));
        }

        return desired;
    }

    private static string ComputeRestDayRotationContentHash(DayOfWeek dayOfWeek, int minFree, int windowWeeks)
    {
        return ImportContentHasher.ComputeHash(
            dayOfWeek.ToString(),
            minFree.ToString(CultureInfo.InvariantCulture),
            windowWeeks.ToString(CultureInfo.InvariantCulture));
    }

    private async Task<(IReadOnlyList<EntityImportDecision<RestDayRotationImportValues>> Decisions, Dictionary<string, RestDayRotationRule> ExistingBySourceKey)> PlanRestDayRotationImportAsync(
        IReadOnlyList<EntityImportDesired<RestDayRotationImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _restDayRotationRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeRestDayRotationContentHash(r.DayOfWeek, r.MinFreeCount, r.WindowWeeks) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    private void ApplyRestDayRotationDecisions(
        IReadOnlyList<EntityImportDecision<RestDayRotationImportValues>> decisions,
        IReadOnlyDictionary<string, RestDayRotationRule> existingBySourceKey,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _restDayRotationRuleRepository.Add(new RestDayRotationRule
                    {
                        Id = Guid.NewGuid(),
                        DayOfWeek = decision.Values.DayOfWeek,
                        MinFreeCount = decision.Values.MinFree,
                        WindowWeeks = decision.Values.WindowWeeks,
                        SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey),
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    existing.DayOfWeek = decision.Values.DayOfWeek;
                    existing.MinFreeCount = decision.Values.MinFree;
                    existing.WindowWeeks = decision.Values.WindowWeeks;
                    existing.SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
                    existing.ImportContentHash = decision.ContentHash;
                    _restDayRotationRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: rest-day rotation '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    private sealed record IndustryProfileDesired(
        List<EntityImportDesired<SchedulingRulePresetImportValues>> RulePresets,
        List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> RateRevisions,
        List<EntityImportDesired<QualificationCatalogImportValues>> Qualifications,
        List<EntityImportDesired<PeriodCapRuleImportValues>> BoundPeriodCaps,
        List<EntityImportDesired<RestDayRotationImportValues>> BoundRestDayRotations,
        List<EntityImportDesired<CounterRuleImportValues>> BoundCounterRules,
        Dictionary<string, string> RuleSourceKeyByBoundEntityKey);

    private IndustryProfileDesired BuildIndustryProfileDesired(
        Dictionary<string, RegionSetupIndustryProfile>? industryProfiles)
    {
        var rulePresets = new List<EntityImportDesired<SchedulingRulePresetImportValues>>();
        var rateRevisions = new List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>>();
        var qualifications = new List<EntityImportDesired<QualificationCatalogImportValues>>();
        var boundPeriodCaps = new List<EntityImportDesired<PeriodCapRuleImportValues>>();
        var boundRotations = new List<EntityImportDesired<RestDayRotationImportValues>>();
        var boundCounterRules = new List<EntityImportDesired<CounterRuleImportValues>>();
        var ruleSourceKeyByBoundEntityKey = new Dictionary<string, string>();

        if (industryProfiles == null || industryProfiles.Count == 0)
        {
            return new IndustryProfileDesired(rulePresets, rateRevisions, qualifications, boundPeriodCaps, boundRotations, boundCounterRules, ruleSourceKeyByBoundEntityKey);
        }

        var seenRuleKeys = new HashSet<string>();
        var seenRevisionKeys = new HashSet<string>();
        var seenQualificationKeys = new HashSet<string>();
        var seenBoundEntityKeys = new HashSet<string>();

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

            var blockRuleKeys = new List<string>();
            foreach (var preset in profile.SchedulingRulePresets ?? [])
            {
                var desired = BuildSchedulingRulePresetDesired(industry, preset, seenRuleKeys);
                blockRuleKeys.Add(desired.SourceKey);
                rulePresets.Add(desired);

                foreach (var revision in BuildRateRevisionDesired(industry, preset, desired.SourceKey, seenRevisionKeys))
                {
                    ruleSourceKeyByBoundEntityKey[revision.SourceKey] = desired.SourceKey;
                    rateRevisions.Add(revision);
                }
            }

            foreach (var entry in profile.QualificationCatalog ?? [])
            {
                qualifications.Add(BuildQualificationCatalogDesired(industry, entry, seenQualificationKeys));
            }

            var hasBoundEntities = (profile.PeriodCaps?.Count ?? 0) > 0 || (profile.RestDayRotations?.Count ?? 0) > 0
                || (profile.CounterRules?.Count ?? 0) > 0;
            if (!hasBoundEntities)
            {
                continue;
            }

            if (blockRuleKeys.Count != 1)
            {
                throw new InvalidRequestException(
                    $"Region setup: industryProfiles.{industry} carries periodCaps/restDayRotations but {blockRuleKeys.Count} scheduling rule presets - industry-scoped rules need exactly ONE preset as their binding target.");
            }

            var ruleSourceKey = blockRuleKeys[0];

            var caps = BuildPeriodCapEntityDesired(
                profile.PeriodCaps,
                $"industryProfiles.{industry}.periodCaps",
                $"region-setup:industryProfiles:{industry}:periodCap",
                seenBoundEntityKeys);
            foreach (var cap in caps)
            {
                ruleSourceKeyByBoundEntityKey[cap.SourceKey] = ruleSourceKey;
                boundPeriodCaps.Add(cap);
            }

            var rotations = BuildRestDayRotationDesired(
                profile.RestDayRotations,
                $"industryProfiles.{industry}.restDayRotations",
                $"region-setup:industryProfiles:{industry}:restDayRotation",
                seenBoundEntityKeys);
            foreach (var rotation in rotations)
            {
                ruleSourceKeyByBoundEntityKey[rotation.SourceKey] = ruleSourceKey;
                boundRotations.Add(rotation);
            }

            var counters = BuildCounterRuleDesired(
                profile.CounterRules,
                $"industryProfiles.{industry}.counterRules",
                $"region-setup:industryProfiles:{industry}:counterRule",
                seenBoundEntityKeys);
            foreach (var counter in counters)
            {
                ruleSourceKeyByBoundEntityKey[counter.SourceKey] = ruleSourceKey;
                boundCounterRules.Add(counter);
            }
        }

        return new IndustryProfileDesired(rulePresets, rateRevisions, qualifications, boundPeriodCaps, boundRotations, boundCounterRules, ruleSourceKeyByBoundEntityKey);
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

        var overtime = ParsePresetOvertime(preset.Overtime, fieldPrefix);

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
            preset.PerformsShiftWork,
            overtime.Basis,
            overtime.RateMode,
            overtime.Tier1AfterHours,
            overtime.Tier1Rate,
            overtime.Tier2AfterHours,
            overtime.Tier2Rate,
            overtime.Tier3AfterHours,
            overtime.Tier3Rate,
            industry);

        return new EntityImportDesired<SchedulingRulePresetImportValues>(
            sourceKey,
            ComputeSchedulingRulePresetContentHash(values),
            values);
    }

    // Each revision is a full snapshot of the five surcharge rates AND the overtime tier ladder effective
    // from validFrom onward, keyed by the parent preset source key plus the ISO date. validFrom must be
    // strictly ascending across a preset's revisions (this also guarantees the :rev:{date} keys are unique)
    // and at least one rate or an overtime tier must be set, otherwise the row would be dead configuration.
    // Overtime is validated by the same ParsePresetOvertime the preset uses (fixedPerShift rejected, at
    // most three strictly-ascending tiers).
    private List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> BuildRateRevisionDesired(
        string industry, RegionSetupSchedulingRulePreset preset, string presetSourceKey, HashSet<string> seenKeys)
    {
        var result = new List<EntityImportDesired<SchedulingRuleRateRevisionImportValues>>();
        if (preset.RateRevisions == null || preset.RateRevisions.Count == 0)
        {
            return result;
        }

        var fieldPrefix = $"industryProfiles.{industry}.schedulingRulePresets.rateRevisions";
        var previousValidFrom = DateOnly.MinValue;
        var hasPrevious = false;

        for (var i = 0; i < preset.RateRevisions.Count; i++)
        {
            var revision = preset.RateRevisions[i];
            if (string.IsNullOrWhiteSpace(revision.ValidFrom))
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}] requires a 'validFrom' date ({RateRevisionDateFormat}).");
            }

            if (!DateOnly.TryParseExact(revision.ValidFrom.Trim(), RateRevisionDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var validFrom))
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].validFrom '{revision.ValidFrom}' is not a valid {RateRevisionDateFormat} date.");
            }

            if (hasPrevious && validFrom <= previousValidFrom)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].validFrom must be strictly ascending across a preset's revisions.");
            }

            RequireNonNegative(revision.NightRate, $"{fieldPrefix}[{i}].nightRate");
            RequireNonNegative(revision.HolidayRate, $"{fieldPrefix}[{i}].holidayRate");
            RequireNonNegative(revision.We1Rate, $"{fieldPrefix}[{i}].we1Rate");
            RequireNonNegative(revision.We2Rate, $"{fieldPrefix}[{i}].we2Rate");
            RequireNonNegative(revision.We3Rate, $"{fieldPrefix}[{i}].we3Rate");

            var overtime = ParsePresetOvertime(revision.Overtime, $"{fieldPrefix}[{i}]");

            var hasAnyRate = revision.NightRate.HasValue || revision.HolidayRate.HasValue
                || revision.We1Rate.HasValue || revision.We2Rate.HasValue || revision.We3Rate.HasValue;
            if (!hasAnyRate && !overtime.Tier1AfterHours.HasValue)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}] must set at least one surcharge rate or an overtime tier.");
            }

            var sourceKey = $"{presetSourceKey}:rev:{validFrom.ToString(RateRevisionDateFormat, CultureInfo.InvariantCulture)}";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: industryProfiles contains more than one rate revision resolving to key '{sourceKey}'.");
            }

            var values = new SchedulingRuleRateRevisionImportValues(
                validFrom,
                revision.NightRate,
                revision.HolidayRate,
                revision.We1Rate,
                revision.We2Rate,
                revision.We3Rate,
                overtime.Basis,
                overtime.RateMode,
                overtime.Tier1AfterHours,
                overtime.Tier1Rate,
                overtime.Tier2AfterHours,
                overtime.Tier2Rate,
                overtime.Tier3AfterHours,
                overtime.Tier3Rate);

            result.Add(new EntityImportDesired<SchedulingRuleRateRevisionImportValues>(
                sourceKey,
                ComputeRateRevisionContentHash(values),
                values));

            previousValidFrom = validFrom;
            hasPrevious = true;
        }

        return result;
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
            IndustryQualificationCategoryMap.Resolve(industry),
            industry);

        return new EntityImportDesired<QualificationCatalogImportValues>(
            sourceKey,
            ComputeQualificationContentHash(values.NameDe, values.NameEn, values.NameFr, values.NameIt, values.IsTimeLimited, values.Category),
            values);
    }

    private sealed record PresetOvertimeValues(
        OvertimeBasis? Basis,
        SurchargeRateMode? RateMode,
        decimal? Tier1AfterHours,
        decimal? Tier1Rate,
        decimal? Tier2AfterHours,
        decimal? Tier2Rate,
        decimal? Tier3AfterHours,
        decimal? Tier3Rate);

    // A preset that declares overtime must carry at least tier 1 - basis/rateMode without any tier would
    // be dead configuration the calculator never reads (a rule overrides the global settings only when
    // its tier 1 is complete).
    private static PresetOvertimeValues ParsePresetOvertime(RegionSetupOvertime? overtime, string fieldPrefix)
    {
        if (overtime == null)
        {
            return new PresetOvertimeValues(null, null, null, null, null, null, null, null);
        }

        OvertimeBasis? basis = null;
        if (!string.IsNullOrWhiteSpace(overtime.Basis))
        {
            basis = ValidateOvertimeBasis(overtime.Basis, $"{fieldPrefix}.overtime.basis") == OvertimeBasisWeek
                ? Klacks.Api.Domain.Enums.OvertimeBasis.Week
                : Klacks.Api.Domain.Enums.OvertimeBasis.Day;
        }

        SurchargeRateMode? rateMode = null;
        if (!string.IsNullOrWhiteSpace(overtime.RateMode))
        {
            rateMode = ValidateOvertimeRateMode(overtime.RateMode, $"{fieldPrefix}.overtime.rateMode") == RateModeFixedPerHour
                ? SurchargeRateMode.FixedPerHour
                : SurchargeRateMode.Multiplier;
        }

        var tiers = ValidateOvertimeTiers(overtime.Tiers, $"{fieldPrefix}.overtime.tiers");
        if (tiers.Count == 0)
        {
            throw new InvalidRequestException(
                $"Region setup: {fieldPrefix}.overtime requires at least one tier (afterHours + rate).");
        }

        return new PresetOvertimeValues(
            basis,
            rateMode,
            tiers.Count > 0 ? tiers[0].AfterHours : null,
            tiers.Count > 0 ? tiers[0].Rate : null,
            tiers.Count > 1 ? tiers[1].AfterHours : null,
            tiers.Count > 1 ? tiers[1].Rate : null,
            tiers.Count > 2 ? tiers[2].AfterHours : null,
            tiers.Count > 2 ? tiers[2].Rate : null);
    }

    private static List<(decimal AfterHours, decimal Rate)> ValidateOvertimeTiers(
        List<RegionSetupOvertimeTier>? tiers, string fieldPrefix)
    {
        var result = new List<(decimal AfterHours, decimal Rate)>();
        if (tiers == null)
        {
            return result;
        }

        if (tiers.Count > MaxOvertimeTiers)
        {
            throw new InvalidRequestException(
                $"Region setup: {fieldPrefix} supports at most {MaxOvertimeTiers} entries (Overtime1/2/3), found {tiers.Count}.");
        }

        var previousAfterHours = decimal.MinValue;
        for (var i = 0; i < tiers.Count; i++)
        {
            var tier = tiers[i];
            if (tier.AfterHours == null || tier.Rate == null)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}] requires 'afterHours' and 'rate'.");
            }

            if (tier.AfterHours <= previousAfterHours)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].afterHours must be strictly ascending across tiers.");
            }

            if (tier.Rate <= 0)
            {
                throw new InvalidRequestException($"Region setup: {fieldPrefix}[{i}].rate must be greater than zero.");
            }

            previousAfterHours = tier.AfterHours.Value;
            result.Add((tier.AfterHours.Value, tier.Rate.Value));
        }

        return result;
    }

    // Used BOTH for the desired hash from the profile file AND for recomputing a stored row's live-value
    // hash - the two calls must format every field identically (see ComputePeriodCapContentHash).
    private static string ComputeSchedulingRulePresetContentHash(SchedulingRulePresetImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.Name,
            FormatInt(values.MaxWorkDays),
            FormatDecimal(values.MinRestDays),
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
            FormatBool(values.PerformsShiftWork),
            values.OvertimeBasis?.ToString() ?? string.Empty,
            values.OvertimeRateMode?.ToString() ?? string.Empty,
            FormatDecimal(values.OvertimeTier1AfterHours),
            FormatDecimal(values.OvertimeTier1Rate),
            FormatDecimal(values.OvertimeTier2AfterHours),
            FormatDecimal(values.OvertimeTier2Rate),
            FormatDecimal(values.OvertimeTier3AfterHours),
            FormatDecimal(values.OvertimeTier3Rate));
    }

    // Used BOTH for the desired hash from the profile file AND for recomputing a stored row's live-value
    // hash (PlanRateRevisionImportAsync) - both calls must format every field identically.
    private static string ComputeRateRevisionContentHash(SchedulingRuleRateRevisionImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.ValidFrom.ToString(RateRevisionDateFormat, CultureInfo.InvariantCulture),
            FormatDecimal(values.NightRate),
            FormatDecimal(values.HolidayRate),
            FormatDecimal(values.We1Rate),
            FormatDecimal(values.We2Rate),
            FormatDecimal(values.We3Rate),
            values.OvertimeBasis?.ToString() ?? string.Empty,
            values.OvertimeRateMode?.ToString() ?? string.Empty,
            FormatDecimal(values.OvertimeTier1AfterHours),
            FormatDecimal(values.OvertimeTier1Rate),
            FormatDecimal(values.OvertimeTier2AfterHours),
            FormatDecimal(values.OvertimeTier2Rate),
            FormatDecimal(values.OvertimeTier3AfterHours),
            FormatDecimal(values.OvertimeTier3Rate));
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
    private const int MaxImportSlugLength = 120;

    private static string BuildImportSlug(string value)
    {
        var trimmed = value.Trim().ToLowerInvariant();
        var slug = string.Join('-', trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (slug.Length <= MaxImportSlugLength)
        {
            return slug;
        }

        // Keep the persisted import source key within its column length (varchar(200)) even for very
        // long free-text names: truncate the slug and append a stable content hash so uniqueness holds.
        var hashSuffix = ImportContentHasher.ComputeHash(value)[..8].ToLowerInvariant();
        return string.Concat(slug.AsSpan(0, MaxImportSlugLength - hashSuffix.Length - 1), "-", hashSuffix);
    }

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
        rule.PerformsShiftWork,
        rule.OvertimeBasis,
        rule.OvertimeRateMode,
        rule.OvertimeTier1AfterHours,
        rule.OvertimeTier1Rate,
        rule.OvertimeTier2AfterHours,
        rule.OvertimeTier2Rate,
        rule.OvertimeTier3AfterHours,
        rule.OvertimeTier3Rate,
        rule.Industry);

    // Besides the source-key/rule-id map this reports the ids of PRE-EXISTING rules whose
    // surcharge-relevant fields (rates, night window, overtime configuration) actually changed.
    // Freshly inserted rules are not reported: no contract can reference them yet, so no persisted
    // work surcharge can depend on them. A no-op re-import reports nothing.
    private (Dictionary<string, Guid> RuleIdBySourceKey, List<Guid> ChangedSurchargeRuleIds) ApplySchedulingRulePresetDecisions(
        IReadOnlyList<EntityImportDecision<SchedulingRulePresetImportValues>> decisions,
        IReadOnlyDictionary<string, SchedulingRule> existingBySourceKey)
    {
        var ruleIdBySourceKey = new Dictionary<string, Guid>();
        var changedSurchargeRuleIds = new List<Guid>();

        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    var rule = new SchedulingRule { Id = Guid.NewGuid(), ImportSourceKey = decision.SourceKey };
                    CopyPresetValues(decision.Values, rule);
                    rule.ImportContentHash = decision.ContentHash;
                    _schedulingRuleImportRepository.Add(rule);
                    ruleIdBySourceKey[decision.SourceKey] = rule.Id;
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    var surchargeFieldsChanged = PresetSurchargeFieldsDiffer(existing, decision.Values);
                    CopyPresetValues(decision.Values, existing);
                    existing.ImportContentHash = decision.ContentHash;
                    _schedulingRuleImportRepository.Update(existing);
                    ruleIdBySourceKey[decision.SourceKey] = existing.Id;
                    if (surchargeFieldsChanged)
                    {
                        changedSurchargeRuleIds.Add(existing.Id);
                    }
                    break;
                case EntityImportAction.SkipEdited:
                    ruleIdBySourceKey[decision.SourceKey] = existingBySourceKey[decision.SourceKey].Id;
                    _logger.LogInformation(
                        "Region setup: scheduling rule preset '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }

        return (ruleIdBySourceKey, changedSurchargeRuleIds);
    }

    private static bool PresetSurchargeFieldsDiffer(SchedulingRule existing, SchedulingRulePresetImportValues values)
    {
        return existing.NightRate != values.NightRate
            || existing.HolidayRate != values.HolidayRate
            || existing.WE1Rate != values.We1Rate
            || existing.WE2Rate != values.We2Rate
            || existing.WE3Rate != values.We3Rate
            || existing.NightStart != values.NightStart
            || existing.NightEnd != values.NightEnd
            || existing.OvertimeThreshold != values.OvertimeThreshold
            || existing.OvertimeBasis != values.OvertimeBasis
            || existing.OvertimeRateMode != values.OvertimeRateMode
            || existing.OvertimeTier1AfterHours != values.OvertimeTier1AfterHours
            || existing.OvertimeTier1Rate != values.OvertimeTier1Rate
            || existing.OvertimeTier2AfterHours != values.OvertimeTier2AfterHours
            || existing.OvertimeTier2Rate != values.OvertimeTier2Rate
            || existing.OvertimeTier3AfterHours != values.OvertimeTier3AfterHours
            || existing.OvertimeTier3Rate != values.OvertimeTier3Rate;
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
        rule.OvertimeBasis = values.OvertimeBasis;
        rule.OvertimeRateMode = values.OvertimeRateMode;
        rule.OvertimeTier1AfterHours = values.OvertimeTier1AfterHours;
        rule.OvertimeTier1Rate = values.OvertimeTier1Rate;
        rule.OvertimeTier2AfterHours = values.OvertimeTier2AfterHours;
        rule.OvertimeTier2Rate = values.OvertimeTier2Rate;
        rule.OvertimeTier3AfterHours = values.OvertimeTier3AfterHours;
        rule.OvertimeTier3Rate = values.OvertimeTier3Rate;
        rule.Industry = values.Industry;
    }

    private async Task<(IReadOnlyList<EntityImportDecision<SchedulingRuleRateRevisionImportValues>> Decisions, Dictionary<string, SchedulingRuleRateRevision> ExistingBySourceKey)> PlanRateRevisionImportAsync(
        IReadOnlyList<EntityImportDesired<SchedulingRuleRateRevisionImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _schedulingRuleRateRevisionImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r =>
            {
                var values = ToImportValues(r);
                return ComputeRateRevisionContentHash(values) == r.ImportContentHash
                    || ComputeRateRevisionLegacyContentHash(values) == r.ImportContentHash;
            });

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    // Legacy fallback: Phase 1 hashed a rate revision over the six surcharge fields only, before Phase 2
    // appended the eight overtime fields to ComputeRateRevisionContentHash. A row imported under Phase 1
    // whose stored hash still matches this six-field digest is unedited, not customer-edited - without
    // this fallback the widened hasher would freeze it as SkipEdited on the next import. Self-limiting: an
    // unedited match is planned as Update, which rewrites the stored hash with the full 14-field digest,
    // so this branch never fires again for that row.
    private static string ComputeRateRevisionLegacyContentHash(SchedulingRuleRateRevisionImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.ValidFrom.ToString(RateRevisionDateFormat, CultureInfo.InvariantCulture),
            FormatDecimal(values.NightRate),
            FormatDecimal(values.HolidayRate),
            FormatDecimal(values.We1Rate),
            FormatDecimal(values.We2Rate),
            FormatDecimal(values.We3Rate));
    }

    private static SchedulingRuleRateRevisionImportValues ToImportValues(SchedulingRuleRateRevision revision) => new(
        revision.ValidFrom,
        revision.NightRate,
        revision.HolidayRate,
        revision.WE1Rate,
        revision.WE2Rate,
        revision.WE3Rate,
        revision.OvertimeBasis,
        revision.OvertimeRateMode,
        revision.OvertimeTier1AfterHours,
        revision.OvertimeTier1Rate,
        revision.OvertimeTier2AfterHours,
        revision.OvertimeTier2Rate,
        revision.OvertimeTier3AfterHours,
        revision.OvertimeTier3Rate);

    // Returns the (rule, ValidFrom) pairs whose revision content ACTUALLY changed. An Update whose
    // desired content hash equals the hash of the row's current live values is a no-op re-import (it
    // may still refresh a legacy stored hash) and is deliberately not reported, so a repeated import
    // of an unchanged file never triggers a surcharge recalculation.
    private List<RateRevisionChange> ApplyRateRevisionDecisions(
        IReadOnlyList<EntityImportDecision<SchedulingRuleRateRevisionImportValues>> decisions,
        IReadOnlyDictionary<string, SchedulingRuleRateRevision> existingBySourceKey,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        var changes = new List<RateRevisionChange>();

        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    var revision = new SchedulingRuleRateRevision
                    {
                        Id = Guid.NewGuid(),
                        SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey) ?? throw new InvalidOperationException($"Region setup: unresolved scheduling rule for rate revision '{decision.SourceKey}'."),
                        ImportSourceKey = decision.SourceKey,
                    };
                    CopyRateRevisionValues(decision.Values, revision);
                    revision.ImportContentHash = decision.ContentHash;
                    _schedulingRuleRateRevisionImportRepository.Add(revision);
                    changes.Add(new RateRevisionChange(revision.SchedulingRuleId, revision.ValidFrom));
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    var contentChanged = ComputeRateRevisionContentHash(ToImportValues(existing)) != decision.ContentHash;
                    var earliestValidFrom = existing.ValidFrom < decision.Values.ValidFrom ? existing.ValidFrom : decision.Values.ValidFrom;
                    existing.SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey) ?? existing.SchedulingRuleId;
                    CopyRateRevisionValues(decision.Values, existing);
                    existing.ImportContentHash = decision.ContentHash;
                    _schedulingRuleRateRevisionImportRepository.Update(existing);
                    if (contentChanged)
                    {
                        changes.Add(new RateRevisionChange(existing.SchedulingRuleId, earliestValidFrom));
                    }
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: scheduling rule rate revision '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }

        return changes;
    }

    private sealed record RateRevisionChange(Guid RuleId, DateOnly ValidFrom);

    // Post-commit hook: raises SchedulingRuleRateRevisionsImportedEvent only when the committed import
    // actually changed rate revisions or surcharge-relevant rule preset fields. Fully defensive - a
    // failing recalculation hook must never break application startup, the import is already committed.
    private async Task DispatchRateRevisionsImportedAsync(
        IReadOnlyList<RateRevisionChange> rateRevisionChanges,
        IReadOnlyList<Guid> changedSurchargeRuleIds)
    {
        if (rateRevisionChanges.Count == 0 && changedSurchargeRuleIds.Count == 0)
        {
            return;
        }

        try
        {
            var ruleIds = rateRevisionChanges.Select(c => c.RuleId)
                .Concat(changedSurchargeRuleIds)
                .Distinct()
                .ToList();
            DateOnly? earliestChangedValidFrom = rateRevisionChanges.Count > 0
                ? rateRevisionChanges.Min(c => c.ValidFrom)
                : null;

            await _eventDispatcher.DispatchAsync(
                new SchedulingRuleRateRevisionsImportedEvent(ruleIds, earliestChangedValidFrom),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed after region setup import; the import is persisted and remains unaffected.",
                nameof(SchedulingRuleRateRevisionsImportedEvent));
        }
    }

    // Compares each planned surcharge-relevant setting against its stored value BEFORE the write.
    // The comparison must happen pre-write: UpsertSettingAsync mutates the tracked Settings entity,
    // so reading it after the write always reports "unchanged". Computed once outside the transaction
    // lambda, so an EF execution-strategy retry cannot skew the result either.
    private async Task<List<string>> ComputeChangedSurchargeRelevantKeysAsync(
        IReadOnlyList<(string Type, string Value)> plannedSettings)
    {
        var changedKeys = new List<string>();
        foreach (var (type, value) in plannedSettings)
        {
            if (!SurchargeRelevantSettingKeys.All.Contains(type) || changedKeys.Contains(type))
            {
                continue;
            }

            var previousValue = (await _settingsRepository.GetSettingNoTracking(type))?.Value;
            if (!string.Equals(previousValue, value, StringComparison.Ordinal))
            {
                changedKeys.Add(type);
            }
        }

        return changedKeys;
    }

    // Post-commit hook: raises SurchargeSettingsChangedEvent only when the committed import actually
    // changed surcharge-relevant global settings. Fully defensive - a failing recalculation hook must
    // never break application startup, the settings are already committed.
    private async Task DispatchSurchargeSettingsChangedAsync(IReadOnlyList<string> changedKeys)
    {
        if (changedKeys.Count == 0)
        {
            return;
        }

        try
        {
            await _eventDispatcher.DispatchAsync(
                new SurchargeSettingsChangedEvent(changedKeys),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Post-commit dispatch of {EventName} failed after region setup import; the settings are persisted and remain unaffected.",
                nameof(SurchargeSettingsChangedEvent));
        }
    }

    private static void CopyRateRevisionValues(SchedulingRuleRateRevisionImportValues values, SchedulingRuleRateRevision revision)
    {
        revision.ValidFrom = values.ValidFrom;
        revision.NightRate = values.NightRate;
        revision.HolidayRate = values.HolidayRate;
        revision.WE1Rate = values.We1Rate;
        revision.WE2Rate = values.We2Rate;
        revision.WE3Rate = values.We3Rate;
        revision.OvertimeBasis = values.OvertimeBasis;
        revision.OvertimeRateMode = values.OvertimeRateMode;
        revision.OvertimeTier1AfterHours = values.OvertimeTier1AfterHours;
        revision.OvertimeTier1Rate = values.OvertimeTier1Rate;
        revision.OvertimeTier2AfterHours = values.OvertimeTier2AfterHours;
        revision.OvertimeTier2Rate = values.OvertimeTier2Rate;
        revision.OvertimeTier3AfterHours = values.OvertimeTier3AfterHours;
        revision.OvertimeTier3Rate = values.OvertimeTier3Rate;
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
                        Industry = decision.Values.Industry,
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
                    existing.Industry = decision.Values.Industry;
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

    private List<EntityImportDesired<MacroImportValues>> BuildMacroDesired(List<RegionSetupMacro>? macros)
    {
        if (macros == null || macros.Count == 0)
        {
            return [];
        }

        var seenKeys = new HashSet<string>();
        var seenFunctions = new HashSet<(MacroCategoryEnum Category, MacroFunctionEnum Function)>();
        var desired = new List<EntityImportDesired<MacroImportValues>>();

        foreach (var macro in macros)
        {
            if (string.IsNullOrWhiteSpace(macro.Name) || string.IsNullOrWhiteSpace(macro.Content))
            {
                throw new InvalidRequestException("Region setup: macros entry requires a non-empty 'name' and 'content'.");
            }

            ValidateMacroScript(macro.Name!, macro.Content!);

            var function = ParseMacroFunction(macro.Function, $"macros ('{macro.Name}').function");
            var category = ParseMacroCategory(macro.Category, $"macros ('{macro.Name}').category");

            if (function != MacroFunctionEnum.Custom && !seenFunctions.Add((category, function)))
            {
                throw new InvalidRequestException(
                    $"Region setup: macros contains more than one entry with function '{function}' for category '{category}'.");
            }

            var name = macro.Name!.Trim();
            var sourceKey = $"region-setup:macros:{BuildImportSlug(name)}";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: macros contains more than one entry resolving to key '{sourceKey}'.");
            }

            var values = new MacroImportValues(name, macro.Content!, category, function);
            desired.Add(new EntityImportDesired<MacroImportValues>(
                sourceKey,
                ComputeMacroContentHash(values.Name, values.Content, values.Category, values.Function),
                values));
        }

        return desired;
    }

    // Delegates to the shared IMacroScriptValidator (compile + probe-execute inside a hard timeout,
    // because static compilation alone misses parser hangs and most malformed input) and converts a
    // validation failure into a fail-fast import error.
    private void ValidateMacroScript(string name, string content)
    {
        var validation = _macroScriptValidator.Validate(content);
        if (!validation.IsValid)
        {
            throw new InvalidRequestException(
                $"Region setup: macros entry '{name}' failed validation: {validation.ErrorMessage}");
        }
    }

    private static string ComputeMacroContentHash(string name, string content, MacroCategoryEnum category, MacroFunctionEnum function)
    {
        return ImportContentHasher.ComputeHash(name, content, category.ToString(), function.ToString());
    }

    private static MacroFunctionEnum ParseMacroFunction(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MacroFunctionEnum.Custom;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "custom" => MacroFunctionEnum.Custom,
            "standard" => MacroFunctionEnum.Standard,
            "standardadditive" => MacroFunctionEnum.StandardAdditive,
            _ => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be 'custom', 'standard' or 'standardAdditive'."),
        };
    }

    private static MacroCategoryEnum ParseMacroCategory(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return MacroCategoryEnum.Shift;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "shift" => MacroCategoryEnum.Shift,
            "vacation" => MacroCategoryEnum.Vacation,
            "illness" => MacroCategoryEnum.Illness,
            "accident" => MacroCategoryEnum.Accident,
            "workshoppaid" => MacroCategoryEnum.WorkshopPaid,
            "workshopunpaid" => MacroCategoryEnum.WorkshopUnpaid,
            _ => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be 'shift', 'vacation', 'illness', 'accident', 'workshopPaid' or 'workshopUnpaid'."),
        };
    }

    // Importing a non-custom function may displace the CURRENT holder of that (category, function) slot,
    // but only when that holder is one of the seeded standard macros or itself an unedited import - a
    // customer-created (or customer-edited) holder is never demoted silently; the import fails with a
    // message telling the operator to resolve the conflict deliberately. Demotions are planned here
    // (fail-fast before any write) and executed first inside the transaction, so the partial unique
    // index (category, type) never sees two active holders.
    private async Task<(IReadOnlyList<EntityImportDecision<MacroImportValues>> Decisions, Dictionary<string, Macro> ExistingBySourceKey, List<Macro> Demotions)> PlanMacroImportAsync(
        IReadOnlyList<EntityImportDesired<MacroImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], [], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _macroImportRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(m => m.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            m => m.ImportSourceKey,
            m => ComputeMacroContentHash(m.Name, m.Content, m.Category, (MacroFunctionEnum)m.Type) == m.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);

        var demotions = new List<Macro>();
        foreach (var decision in decisions)
        {
            if (decision.Action == EntityImportAction.SkipEdited || decision.Values.Function == MacroFunctionEnum.Custom)
            {
                continue;
            }

            var holder = await _macroImportRepository.GetActiveFunctionHolderAsync(decision.Values.Category, decision.Values.Function);
            if (holder == null || holder.ImportSourceKey == decision.SourceKey)
            {
                continue;
            }

            var isSeeded = holder.Id == SeededMacroIds.AllShift || holder.Id == SeededMacroIds.AllShiftAdditive;
            var isUneditedImport = holder.ImportSourceKey.Length > 0
                && ComputeMacroContentHash(holder.Name, holder.Content, holder.Category, (MacroFunctionEnum)holder.Type) == holder.ImportContentHash;
            if (!isSeeded && !isUneditedImport)
            {
                throw new InvalidRequestException(
                    $"Region setup: macro '{decision.Values.Name}' wants function '{decision.Values.Function}' for category '{decision.Values.Category}', but customer macro '{holder.Name}' currently carries it. Set that macro's function to Custom first, then re-run the setup.");
            }

            demotions.Add(holder);
        }

        return (decisions, existingBySourceKey, demotions);
    }

    // Flushed via CompleteAsync BEFORE the new function holders are inserted: relying on EF Core's
    // internal statement ordering against the partial unique index (category, type) is fragile
    // (dotnet/efcore#28065/#30601 document exactly this filtered-index constellation), and a violation
    // here would crash the whole application start. Both flushes run inside the same outer transaction,
    // so atomicity is preserved.
    private async Task ApplyMacroDemotionsAsync(IReadOnlyList<Macro> demotions)
    {
        if (demotions.Count == 0)
        {
            return;
        }

        foreach (var demoted in demotions)
        {
            demoted.Type = (int)MacroFunctionEnum.Custom;
            _macroImportRepository.Update(demoted);
            _logger.LogInformation(
                "Region setup: macro '{Name}' demoted to Custom - the imported profile ships its own holder for that function",
                demoted.Name);
        }

        await _unitOfWork.CompleteAsync();
    }

    private void ApplyMacroDecisions(
        IReadOnlyList<EntityImportDecision<MacroImportValues>> decisions,
        IReadOnlyDictionary<string, Macro> existingBySourceKey)
    {
        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _macroImportRepository.Add(new Macro
                    {
                        Id = Guid.NewGuid(),
                        Name = decision.Values.Name,
                        Content = decision.Values.Content,
                        Category = decision.Values.Category,
                        Type = (int)decision.Values.Function,
                        Description = new MultiLanguage(),
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    existing.Name = decision.Values.Name;
                    existing.Content = decision.Values.Content;
                    existing.Category = decision.Values.Category;
                    existing.Type = (int)decision.Values.Function;
                    existing.ImportContentHash = decision.ContentHash;
                    _macroImportRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: macro '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    private static List<EntityImportDesired<CounterRuleImportValues>> BuildCounterRuleDesired(
        List<RegionSetupCounterRule>? counterRules, string sectionPath, string sourceKeyPrefix, HashSet<string>? sharedSeenKeys = null)
    {
        var seenKeys = sharedSeenKeys ?? new HashSet<string>();
        var desired = new List<EntityImportDesired<CounterRuleImportValues>>();
        if (counterRules == null || counterRules.Count == 0)
        {
            return desired;
        }

        foreach (var counter in counterRules)
        {
            if (string.IsNullOrWhiteSpace(counter.Event) || counter.Threshold == null)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath} entry requires 'event' and 'threshold'.");
            }

            var eventType = ParseCounterEvent(counter.Event, $"{sectionPath}.event");
            var period = ParseCounterPeriod(counter.Period, eventType, $"{sectionPath}.period");

            if (counter.Threshold <= 0)
            {
                throw new InvalidRequestException($"Region setup: {sectionPath}.threshold must be greater than zero.");
            }

            if (eventType == CounterEventType.ShiftExceedingHours)
            {
                if (counter.HoursThreshold is not > 0)
                {
                    throw new InvalidRequestException(
                        $"Region setup: {sectionPath} entry with event 'shiftExceedingHours' requires 'hoursThreshold' greater than zero.");
                }
            }
            else if (counter.HoursThreshold != null)
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath}.hoursThreshold is only allowed with event 'shiftExceedingHours'.");
            }

            var hoursSuffix = counter.HoursThreshold.HasValue
                ? $":{counter.HoursThreshold.Value.ToString("0.##", CultureInfo.InvariantCulture)}h"
                : string.Empty;
            var sourceKey = $"{sourceKeyPrefix}:{eventType.ToString().ToLowerInvariant()}:{period.ToString().ToLowerInvariant()}{hoursSuffix}";
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} contains more than one entry resolving to key '{sourceKey}'.");
            }

            var values = new CounterRuleImportValues(eventType, period, counter.Threshold.Value, counter.HoursThreshold);
            desired.Add(new EntityImportDesired<CounterRuleImportValues>(
                sourceKey,
                ComputeCounterRuleContentHash(values.EventType, values.Period, values.Threshold, values.HoursThreshold),
                values));
        }

        return desired;
    }

    private static string ComputeCounterRuleContentHash(CounterEventType eventType, CounterPeriod period, int threshold, decimal? hoursThreshold)
    {
        return ImportContentHasher.ComputeHash(
            eventType.ToString(),
            period.ToString(),
            threshold.ToString(CultureInfo.InvariantCulture),
            hoursThreshold?.ToString("F4", CultureInfo.InvariantCulture) ?? string.Empty);
    }

    private static CounterEventType ParseCounterEvent(string value, string fieldName)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "nightshift" => CounterEventType.NightShift,
            "workeddayinweek" => CounterEventType.WorkedDayInWeek,
            "shiftexceedinghours" => CounterEventType.ShiftExceedingHours,
            _ => throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be 'nightShift', 'workedDayInWeek' or 'shiftExceedingHours'."),
        };
    }

    private static CounterPeriod ParseCounterPeriod(string? value, CounterEventType eventType, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return eventType == CounterEventType.WorkedDayInWeek ? CounterPeriod.Week : CounterPeriod.Year;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "week" => CounterPeriod.Week,
            "month" => CounterPeriod.Month,
            "year" => CounterPeriod.Year,
            _ => throw new InvalidRequestException($"Region setup: '{value}' in {fieldName} must be 'week', 'month' or 'year'."),
        };
    }

    private async Task<(IReadOnlyList<EntityImportDecision<CounterRuleImportValues>> Decisions, Dictionary<string, CounterRule> ExistingBySourceKey)> PlanCounterRuleImportAsync(
        IReadOnlyList<EntityImportDesired<CounterRuleImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _counterRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeCounterRuleContentHash(r.EventType, r.Period, r.Threshold, r.HoursThreshold) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    private void ApplyCounterRuleDecisions(
        IReadOnlyList<EntityImportDecision<CounterRuleImportValues>> decisions,
        IReadOnlyDictionary<string, CounterRule> existingBySourceKey,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _counterRuleRepository.Add(new CounterRule
                    {
                        Id = Guid.NewGuid(),
                        EventType = decision.Values.EventType,
                        Period = decision.Values.Period,
                        Threshold = decision.Values.Threshold,
                        HoursThreshold = decision.Values.HoursThreshold,
                        SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey),
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    existing.EventType = decision.Values.EventType;
                    existing.Period = decision.Values.Period;
                    existing.Threshold = decision.Values.Threshold;
                    existing.HoursThreshold = decision.Values.HoursThreshold;
                    existing.SchedulingRuleId = ResolveBoundRuleId(decision.SourceKey, ruleSourceKeyByBoundEntityKey, ruleIdBySourceKey);
                    existing.ImportContentHash = decision.ContentHash;
                    _counterRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: counter rule '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    // K16 restricted time windows. The source key is content-addressed (season + daily times + group tag)
    // like the plan specifies: re-applying the same file finds each row by its stored ImportSourceKey and
    // updates it to identical values (a no-op), while a customer edit to a live value fails the recomputed
    // hash and is left untouched (SkipEdited). KNOWN LIMITATION shared with every K20 rule type: editing an
    // identity field in the profile (here any of season/times/tag) produces a NEW key and leaves the prior
    // import row active - there is no orphan cleanup, so such a change must be paired with removing the old
    // row. No industry (SchedulingRuleId) binding: the group tag is K16's only scope axis.
    private static List<EntityImportDesired<RestrictedTimeWindowImportValues>> BuildRestrictedTimeWindowDesired(
        List<RegionSetupRestrictedTimeWindow>? windows, string sectionPath, string sourceKeyPrefix)
    {
        var seenKeys = new HashSet<string>();
        var desired = new List<EntityImportDesired<RestrictedTimeWindowImportValues>>();
        if (windows == null || windows.Count == 0)
        {
            return desired;
        }

        foreach (var window in windows)
        {
            if (string.IsNullOrWhiteSpace(window.SeasonFrom) || string.IsNullOrWhiteSpace(window.SeasonTo)
                || string.IsNullOrWhiteSpace(window.DailyStart) || string.IsNullOrWhiteSpace(window.DailyEnd))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} entry requires 'seasonFrom', 'seasonTo', 'dailyStart' and 'dailyEnd'.");
            }

            var (fromMonth, fromDay) = ParseMonthDay(window.SeasonFrom, $"{sectionPath}.seasonFrom");
            var (toMonth, toDay) = ParseMonthDay(window.SeasonTo, $"{sectionPath}.seasonTo");
            var dailyStart = ParseTimeOfDay(window.DailyStart, $"{sectionPath}.dailyStart");
            var dailyEnd = ParseTimeOfDay(window.DailyEnd, $"{sectionPath}.dailyEnd");

            var tag = window.AppliesToGroupTag?.Trim() ?? string.Empty;
            var values = new RestrictedTimeWindowImportValues(fromMonth, fromDay, toMonth, toDay, dailyStart, dailyEnd, tag);

            var sourceKey = BuildRestrictedTimeWindowSourceKey(sourceKeyPrefix, values);
            if (!seenKeys.Add(sourceKey))
            {
                throw new InvalidRequestException(
                    $"Region setup: {sectionPath} contains more than one entry resolving to key '{sourceKey}'.");
            }

            desired.Add(new EntityImportDesired<RestrictedTimeWindowImportValues>(
                sourceKey,
                ComputeRestrictedTimeWindowContentHash(values),
                values));
        }

        return desired;
    }

    private static string BuildRestrictedTimeWindowSourceKey(string sourceKeyPrefix, RestrictedTimeWindowImportValues values)
    {
        var tagSuffix = values.AppliesToGroupTag.Length == 0
            ? "*"
            : values.AppliesToGroupTag.ToLowerInvariant();
        var season = string.Create(CultureInfo.InvariantCulture,
            $"{values.SeasonFromMonth:00}-{values.SeasonFromDay:00}:{values.SeasonToMonth:00}-{values.SeasonToDay:00}");
        var times = $"{values.DailyStart.ToString("HHmm", CultureInfo.InvariantCulture)}-{values.DailyEnd.ToString("HHmm", CultureInfo.InvariantCulture)}";
        return $"{sourceKeyPrefix}:{season}:{times}:{tagSuffix}";
    }

    private static string ComputeRestrictedTimeWindowContentHash(RestrictedTimeWindowImportValues values)
    {
        return ImportContentHasher.ComputeHash(
            values.SeasonFromMonth.ToString(CultureInfo.InvariantCulture),
            values.SeasonFromDay.ToString(CultureInfo.InvariantCulture),
            values.SeasonToMonth.ToString(CultureInfo.InvariantCulture),
            values.SeasonToDay.ToString(CultureInfo.InvariantCulture),
            values.DailyStart.ToString("HH:mm", CultureInfo.InvariantCulture),
            values.DailyEnd.ToString("HH:mm", CultureInfo.InvariantCulture),
            values.AppliesToGroupTag);
    }

    private static (int Month, int Day) ParseMonthDay(string value, string fieldName)
    {
        var parts = value.Split('-');
        if (parts.Length != 2
            || !int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var month)
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var day)
            || month < 1 || month > 12 || day < 1 || day > 31)
        {
            throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be a valid 'MM-DD' (month 1-12, day 1-31).");
        }

        return (month, day);
    }

    private static TimeOnly ParseTimeOfDay(string value, string fieldName)
    {
        if (!TimeOnly.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
        {
            throw new InvalidRequestException(
                $"Region setup: '{value}' in {fieldName} must be a valid 'HH:mm' time.");
        }

        return time;
    }

    private async Task<(IReadOnlyList<EntityImportDecision<RestrictedTimeWindowImportValues>> Decisions, Dictionary<string, RestrictedTimeWindowRule> ExistingBySourceKey)> PlanRestrictedTimeWindowImportAsync(
        IReadOnlyList<EntityImportDesired<RestrictedTimeWindowImportValues>> desired)
    {
        if (desired.Count == 0)
        {
            return ([], []);
        }

        var sourceKeys = desired.Select(d => d.SourceKey).ToList();
        var existingRows = await _restrictedTimeWindowRuleRepository.GetBySourceKeysAsync(sourceKeys);
        var existingBySourceKey = existingRows.ToDictionary(r => r.ImportSourceKey);

        var existingUneditedBySourceKey = existingRows.ToDictionary(
            r => r.ImportSourceKey,
            r => ComputeRestrictedTimeWindowContentHash(new RestrictedTimeWindowImportValues(
                r.SeasonFromMonth, r.SeasonFromDay, r.SeasonToMonth, r.SeasonToDay,
                r.DailyStart, r.DailyEnd, r.AppliesToGroupTag)) == r.ImportContentHash);

        var decisions = EntityImportPlanner.Plan(existingUneditedBySourceKey, desired);
        return (decisions, existingBySourceKey);
    }

    private void ApplyRestrictedTimeWindowDecisions(
        IReadOnlyList<EntityImportDecision<RestrictedTimeWindowImportValues>> decisions,
        IReadOnlyDictionary<string, RestrictedTimeWindowRule> existingBySourceKey)
    {
        foreach (var decision in decisions)
        {
            switch (decision.Action)
            {
                case EntityImportAction.Insert:
                    _restrictedTimeWindowRuleRepository.Add(new RestrictedTimeWindowRule
                    {
                        Id = Guid.NewGuid(),
                        SeasonFromMonth = decision.Values.SeasonFromMonth,
                        SeasonFromDay = decision.Values.SeasonFromDay,
                        SeasonToMonth = decision.Values.SeasonToMonth,
                        SeasonToDay = decision.Values.SeasonToDay,
                        DailyStart = decision.Values.DailyStart,
                        DailyEnd = decision.Values.DailyEnd,
                        AppliesToGroupTag = decision.Values.AppliesToGroupTag,
                        ImportSourceKey = decision.SourceKey,
                        ImportContentHash = decision.ContentHash,
                    });
                    break;
                case EntityImportAction.Update:
                    var existing = existingBySourceKey[decision.SourceKey];
                    existing.SeasonFromMonth = decision.Values.SeasonFromMonth;
                    existing.SeasonFromDay = decision.Values.SeasonFromDay;
                    existing.SeasonToMonth = decision.Values.SeasonToMonth;
                    existing.SeasonToDay = decision.Values.SeasonToDay;
                    existing.DailyStart = decision.Values.DailyStart;
                    existing.DailyEnd = decision.Values.DailyEnd;
                    existing.AppliesToGroupTag = decision.Values.AppliesToGroupTag;
                    existing.ImportContentHash = decision.ContentHash;
                    _restrictedTimeWindowRuleRepository.Update(existing);
                    break;
                case EntityImportAction.SkipEdited:
                    _logger.LogInformation(
                        "Region setup: restricted time window '{SourceKey}' was edited by the customer since the last import, skipping re-apply",
                        decision.SourceKey);
                    break;
            }
        }
    }

    // Best-effort heads-up (never a hard failure): the group tag is a plain Group.Name convention, so a rule
    // whose tag matches no existing group stays inert until a group of that name is created and shifts are
    // linked into it. The group may legitimately be created later, so this only logs a warning.
    private async Task WarnOnMissingGroupTagsAsync(
        IReadOnlyList<EntityImportDesired<RestrictedTimeWindowImportValues>> desired)
    {
        var tags = desired
            .Select(d => d.Values.AppliesToGroupTag)
            .Where(t => !string.IsNullOrEmpty(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (tags.Count == 0)
        {
            return;
        }

        var groups = await _groupRepository.List();
        var groupNames = groups
            .Select(g => g.Name)
            .Where(n => !string.IsNullOrEmpty(n))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var tag in tags.Where(tag => !groupNames.Contains(tag)))
        {
            _logger.LogWarning(
                "Region setup: restricted-time-window group tag '{Tag}' matches no existing group; the rule stays inert until a group with that name is created and shifts are linked to it",
                tag);
        }
    }

    // The binding (industry cap/rotation -> block rule) is part of the row's identity, not of its
    // editable values: it is re-applied on every import (also on Update) and never hashed - a stale
    // binding after a file edit is an import concern, not a customer edit.
    private static Guid? ResolveBoundRuleId(
        string entitySourceKey,
        IReadOnlyDictionary<string, string> ruleSourceKeyByBoundEntityKey,
        IReadOnlyDictionary<string, Guid> ruleIdBySourceKey)
    {
        return ruleSourceKeyByBoundEntityKey.TryGetValue(entitySourceKey, out var ruleKey)
               && ruleIdBySourceKey.TryGetValue(ruleKey, out var ruleId)
            ? ruleId
            : null;
    }

    private static string ComputeSha256Hex(string content)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
