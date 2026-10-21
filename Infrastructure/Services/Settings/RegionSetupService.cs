// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// First-run region setup: reads an optional mounted JSON profile, installs language plugins and writes
/// locale, calendar, worktime, surcharge and export settings. Each profile section has its own marker
/// setting so a section is applied exactly once, independently of the others — adding a brand-new section
/// to the schema later still gets applied on an already-configured installation. Any invalid profile
/// content (including an unknown schema version) fails fast before the first write.
/// Known limitation: a field added to an EXISTING section (e.g. surcharges.nightWindow) is not covered by
/// the section marker once that section was already applied on an installation. The night window fields
/// specifically are closed via a dedicated best-effort backfill
/// (<see cref="RegionSetupSettingsBackfiller.BackfillNightWindowIfPresentAsync"/>); the K3/K4
/// surcharges.stackingMode/overtime fields added later get the same treatment
/// (<see cref="RegionSetupSettingsBackfiller.BackfillOvertimeIfPresentAsync"/>); other future field
/// additions to existing sections need the same treatment or a new section of their own.
/// compliance.periodCaps and the top-level industryProfiles map (K20/K5) are ENTITY-import sections, not
/// settings sections: they are never gated by a section marker at all (a per-row
/// ImportSourceKey/ImportContentHash mechanism — see
/// <see cref="Klacks.Api.Application.Services.Setup.EntityImportPlanner"/> and
/// <see cref="RegionEntityImportCoordinator"/> — decides insert/update/skip per row instead), so new or
/// changed rows are reconciled on every ApplyAsync call, even on an installation where every settings
/// section is already fully applied. Deploy-order trap: because RegionSetupProfile disallows unmapped
/// JSON members, mounting a region-setup.json that already contains "compliance.periodCaps" or
/// "industryProfiles" while an OLDER binary (without these DTO fields) is still running makes that older
/// binary reject the entire file — deploy the binary carrying the fields before mounting a file that
/// uses them.
/// The activeIndustries list is a settings section of its own (marker REGION_SETUP_APPLIED_INDUSTRIES,
/// applied exactly once, validated fail-fast against the industryProfiles keys of the same file); the
/// package identity block (package.country/version) is deliberately NEITHER marker-gated nor
/// once-only — it is re-applied on every run, including the fully-applied fast path, so a newer
/// marketplace package version updates REGION_PACKAGE_COUNTRY/REGION_PACKAGE_VERSION.
/// </summary>
/// <param name="configuration">App configuration providing the RegionSetup:File path</param>
/// <param name="settingsRepository">Reads the global marker and upserts the planned settings</param>
/// <param name="unitOfWork">Persists all setting and entity-import writes in one transaction</param>
/// <param name="sectionPlanner">Reads the section markers and plans the marker-gated settings sections</param>
/// <param name="languagePlanner">Installs the planned language plugins</param>
/// <param name="calendarSelectionResolver">Resolves locale.calendarSelection to a calendar selection id</param>
/// <param name="backfiller">Best-effort backfills of fields added to already-applied sections</param>
/// <param name="changeDetector">Finds the surcharge-relevant settings whose value actually changes</param>
/// <param name="entityImports">Builds, plans and applies the entity-import rows of the profile</param>
/// <param name="eventPublisher">Raises the post-commit rate-revision and surcharge-settings events (never on a no-op import)</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

namespace Klacks.Api.Infrastructure.Services.Settings;

public class RegionSetupService : IRegionSetupService
{
    public const string FileConfigKey = RegionSetupFileReader.FileConfigKey;

    private const string SectionAppliedMarkerValue = "true";

    private readonly IConfiguration _configuration;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RegionSetupSectionPlanner _sectionPlanner;
    private readonly RegionSetupLanguagePlanner _languagePlanner;
    private readonly RegionSetupCalendarSelectionResolver _calendarSelectionResolver;
    private readonly RegionSetupSettingsBackfiller _backfiller;
    private readonly RegionSetupSettingsChangeDetector _changeDetector;
    private readonly RegionEntityImportCoordinator _entityImports;
    private readonly RegionSetupEventPublisher _eventPublisher;
    private readonly ILogger<RegionSetupService> _logger;

    public RegionSetupService(
        IConfiguration configuration,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        RegionSetupSectionPlanner sectionPlanner,
        RegionSetupLanguagePlanner languagePlanner,
        RegionSetupCalendarSelectionResolver calendarSelectionResolver,
        RegionSetupSettingsBackfiller backfiller,
        RegionSetupSettingsChangeDetector changeDetector,
        RegionEntityImportCoordinator entityImports,
        RegionSetupEventPublisher eventPublisher,
        ILogger<RegionSetupService> logger)
    {
        _configuration = configuration;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _sectionPlanner = sectionPlanner;
        _languagePlanner = languagePlanner;
        _calendarSelectionResolver = calendarSelectionResolver;
        _backfiller = backfiller;
        _changeDetector = changeDetector;
        _entityImports = entityImports;
        _eventPublisher = eventPublisher;
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

        var sectionMarkerExists = await _sectionPlanner.LoadSectionMarkerExistenceAsync();

        if (sectionMarkerExists[RegionSetupSection.Surcharges])
        {
            await _backfiller.BackfillNightWindowIfPresentAsync(filePath);
            await _backfiller.BackfillOvertimeIfPresentAsync(filePath);
        }

        if (sectionMarkerExists[RegionSetupSection.Compliance])
        {
            await _backfiller.BackfillCompensatoryRestIfPresentAsync(filePath);
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

        var entityPlan = await _entityImports.BuildAndPlanBootImportAsync(profile);

        var actions = RegionSetupSectionPlanner.DetermineSectionActions(profile, sectionMarkerExists, globalMarker != null);
        var settingsPlan = _sectionPlanner.BuildPlan(profile, actions);
        var plannedSettings = settingsPlan.Settings;

        RegionSetupSettingsBuilder.AddPackageIdentitySettings(profile.Package, plannedSettings);

        await _languagePlanner.InstallLanguagesAsync(settingsPlan.LanguagesToInstall);

        if (actions[RegionSetupSection.Locale] == RegionSetupSectionAction.Apply)
        {
            var calendarSelectionId = await _calendarSelectionResolver.ResolveCalendarSelectionIdAsync(profile.Locale?.CalendarSelection);
            if (calendarSelectionId.HasValue)
            {
                plannedSettings.Add((SettingKeys.GlobalCalendarSelectionId, calendarSelectionId.Value.ToString()));
            }
        }

        var markerValue = ComputeSha256Hex(content);

        var changedSurchargeSettingKeys = await _changeDetector.ComputeChangedSurchargeRelevantKeysAsync(plannedSettings);

        var applied = RegionEntityImportApplyResult.None;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (type, value) in plannedSettings)
            {
                await _settingsRepository.UpsertSettingAsync(type, value);
            }

            foreach (var markerKey in settingsPlan.SectionMarkersToWrite)
            {
                await _settingsRepository.UpsertSettingAsync(markerKey, SectionAppliedMarkerValue);
            }

            await _settingsRepository.UpsertSettingAsync(SettingKeys.RegionSetupApplied, markerValue);
            applied = await _entityImports.ApplyAsync(entityPlan);
            await _unitOfWork.CompleteAsync();
            return plannedSettings.Count;
        });

        await _eventPublisher.DispatchRateRevisionsImportedAsync(applied.RateRevisionChanges, applied.ChangedSurchargeRuleIds);
        await _eventPublisher.DispatchSurchargeSettingsChangedAsync(changedSurchargeSettingKeys);

        _logger.LogInformation(
            "Region setup applied: region '{Region}', {LanguageCount} language plugin(s) installed, {SettingCount} setting(s) written, {SectionCount} section marker(s) recorded, {PeriodCapCount} period cap row(s), {RestDayRotationCount} rest-day rotation(s), {RulePresetCount} scheduling rule preset(s) and {QualificationCount} qualification catalog row(s) reconciled",
            profile.Region,
            settingsPlan.LanguagesToInstall.Count,
            plannedSettings.Count,
            settingsPlan.SectionMarkersToWrite.Count,
            entityPlan.PeriodCaps.WriteCount,
            entityPlan.RestDayRotations.WriteCount,
            entityPlan.RulePresets.WriteCount,
            entityPlan.Qualifications.WriteCount);
    }

    // K20 entity-import path for an installation where every settings section is already fully applied
    // (the whole-file/section-marker mechanism has nothing left to do). compliance.periodCaps and
    // industryProfiles are never gated by a section marker, so they still need reconciling here, but a
    // missing or no-longer-valid profile file must never block startup on a repeat run - same tolerance
    // as the night-window backfill, just for entity rows instead of settings.
    private async Task ApplyEntityImportsOnFullyAppliedInstallationAsync(string filePath)
    {
        await _backfiller.ApplyPackageIdentityIfPresentAsync(filePath);

        var desired = await _entityImports.TryBuildDesiredIfPresentAsync(filePath);
        if (desired == null || desired.IsEmpty)
        {
            _logger.LogInformation("Region setup already applied for all known sections, skipping");
            return;
        }

        var plan = await _entityImports.PlanFullyAppliedImportAsync(desired);

        if (!plan.HasWrites)
        {
            _logger.LogInformation(
                "Region setup already applied for all known sections; {Count} entity-import row(s) in the file are unchanged or customer-edited, skipping",
                plan.PeriodCaps.Count + plan.RestDayRotations.Count + plan.RulePresets.Count + plan.Qualifications.Count + plan.Macros.Rows.Count + plan.CounterRules.Count + plan.RestrictedTimeWindows.Count);
            return;
        }

        var applied = RegionEntityImportApplyResult.None;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            applied = await _entityImports.ApplyAsync(plan);
            await _unitOfWork.CompleteAsync();
            return plan.PeriodCaps.Count + plan.RestDayRotations.Count + plan.RulePresets.Count + plan.Qualifications.Count + plan.Macros.Rows.Count;
        });

        await _eventPublisher.DispatchRateRevisionsImportedAsync(applied.RateRevisionChanges, applied.ChangedSurchargeRuleIds);

        _logger.LogInformation(
            "Region setup: reconciled {PeriodCapCount} period cap row(s), {RestDayRotationCount} rest-day rotation(s), {RulePresetCount} scheduling rule preset(s), {QualificationCount} qualification catalog row(s), {CounterRuleCount} counter rule(s) and {RestrictedTimeWindowCount} restricted time window(s) on an installation with all settings sections already applied",
            plan.PeriodCaps.WriteCount,
            plan.RestDayRotations.WriteCount,
            plan.RulePresets.WriteCount,
            plan.Qualifications.WriteCount,
            plan.CounterRules.WriteCount,
            plan.RestrictedTimeWindows.WriteCount);
    }

    private static string ComputeSha256Hex(string content)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));
    }
}
