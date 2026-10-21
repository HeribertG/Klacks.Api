// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// WP6 marketplace auto-update entry point: applies ONLY the parts of a downloaded region profile that
/// are re-import-safe by design - industryProfiles entity imports (rule presets incl. rate revisions,
/// qualification catalogs, industry-BOUND compliance rows), macros and the package identity settings.
/// Deliberately NOT applied here: every once-only settings section
/// (languages/locale/calendar/worktime/surcharges/export/compliance), the top-level compliance.* entity
/// rows, restricted time windows and the ACTIVE_INDUSTRIES setting (admin sovereignty). Reuses the exact
/// per-row ImportSourceKey/ContentHash/SkipEdited plan+apply machinery of the boot path, so
/// customer-edited rows are never overwritten and a customer-held macro function slot fails the import
/// with an InvalidRequestException before any write.
/// </summary>
/// <param name="entityImports">Builds, plans and applies the entity-import rows</param>
/// <param name="changeDetector">Finds the package identity settings whose stored value changes</param>
/// <param name="settingsRepository">Upserts the changed package identity settings</param>
/// <param name="unitOfWork">Persists all writes in one transaction</param>
/// <param name="eventPublisher">Raises the post-commit rate-revision event when rates actually changed</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Settings;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

namespace Klacks.Api.Infrastructure.Services.Settings;

public sealed class RegionEntityImportService : IRegionEntityImportService
{
    private readonly RegionEntityImportCoordinator _entityImports;
    private readonly RegionSetupSettingsChangeDetector _changeDetector;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RegionSetupEventPublisher _eventPublisher;
    private readonly ILogger<RegionEntityImportService> _logger;

    public RegionEntityImportService(
        RegionEntityImportCoordinator entityImports,
        RegionSetupSettingsChangeDetector changeDetector,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        RegionSetupEventPublisher eventPublisher,
        ILogger<RegionEntityImportService> logger)
    {
        _entityImports = entityImports;
        _changeDetector = changeDetector;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task ApplyEntityImportsAsync(RegionSetupProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var plan = await _entityImports.BuildAndPlanPackageImportAsync(profile);

        var packageSettings = new List<(string Type, string Value)>();
        RegionSetupSettingsBuilder.AddPackageIdentitySettings(profile.Package, packageSettings);
        var changedPackageSettings = await _changeDetector.FindChangedSettingsAsync(packageSettings);

        if (changedPackageSettings.Count == 0 && !plan.HasWrites)
        {
            _logger.LogInformation(
                "Region package entity import: all rows are unchanged or customer-edited and the package identity is current, skipping");
            return;
        }

        var applied = RegionEntityImportApplyResult.None;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (type, value) in changedPackageSettings)
            {
                await _settingsRepository.UpsertSettingAsync(type, value);
            }

            applied = await _entityImports.ApplyAsync(plan);
            await _unitOfWork.CompleteAsync();
            return changedPackageSettings.Count + plan.PeriodCaps.Count + plan.RestDayRotations.Count
                + plan.CounterRules.Count + plan.RulePresets.Count + plan.RateRevisions.Count
                + plan.Qualifications.Count + plan.Macros.Rows.Count;
        });

        await _eventPublisher.DispatchRateRevisionsImportedAsync(applied.RateRevisionChanges, applied.ChangedSurchargeRuleIds);

        _logger.LogInformation(
            "Region package entity import: reconciled {RulePresetCount} scheduling rule preset(s), {RateRevisionCount} rate revision(s), {QualificationCount} qualification catalog row(s), {PeriodCapCount} bound period cap(s), {RestDayRotationCount} bound rest-day rotation(s), {CounterRuleCount} bound counter rule(s), {MacroCount} macro(s) and {PackageSettingCount} package identity setting(s)",
            plan.RulePresets.WriteCount,
            plan.RateRevisions.WriteCount,
            plan.Qualifications.WriteCount,
            plan.PeriodCaps.WriteCount,
            plan.RestDayRotations.WriteCount,
            plan.CounterRules.WriteCount,
            plan.Macros.Rows.WriteCount,
            changedPackageSettings.Count);
    }
}
