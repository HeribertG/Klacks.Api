// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Best-effort passes that close the known gap of the per-section marker model: a field added to an
/// EXISTING section later (surcharges.nightWindow, surcharges.stackingMode/overtime, compliance
/// compensatoryRest) never reaches an installation whose section marker is already set, so each pass
/// writes only settings that are still absent. The package identity is re-synchronised on every start
/// of a fully applied installation. Any failure is logged and swallowed: an already-configured
/// installation must never fail to start over a profile file it no longer needs.
/// </summary>
/// <param name="settingsRepository">Reads and upserts rows of the settings table</param>
/// <param name="unitOfWork">Persists each backfill in its own transaction</param>
/// <param name="changeDetector">Finds the settings whose stored value actually changes</param>
/// <param name="eventPublisher">Raises SurchargeSettingsChangedEvent after a committed surcharge backfill</param>
/// <param name="logger">Logger instance for diagnostic output</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RegionSetupSettingsBackfiller
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly RegionSetupSettingsChangeDetector _changeDetector;
    private readonly RegionSetupEventPublisher _eventPublisher;
    private readonly ILogger<RegionSetupSettingsBackfiller> _logger;

    public RegionSetupSettingsBackfiller(
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        RegionSetupSettingsChangeDetector changeDetector,
        RegionSetupEventPublisher eventPublisher,
        ILogger<RegionSetupSettingsBackfiller> logger)
    {
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _changeDetector = changeDetector;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    // Known gap of the per-section marker model (see class summary): once REGION_SETUP_APPLIED_SURCHARGES
    // exists, the surcharges section is never re-applied, so a field added to that section later (like
    // nightWindow) never reaches an installation that already ran setup. This best-effort pass closes that
    // gap for exactly this field: it writes SURCHARGE_NIGHT_START/END only when both the DB value is still
    // absent and the file provides it, independent of the section marker. Any failure (missing file,
    // invalid JSON, invalid time format) is logged and swallowed rather than propagated, because an
    // already-configured installation must never fail to start over a profile file it no longer needs.
    public async Task BackfillNightWindowIfPresentAsync(string filePath)
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
                RegionSetupFieldParser.ValidateTimeOfDay(nightWindow.Start, "surcharges.nightWindow.start");
                toWrite.Add((SettingKeys.SurchargeNightStart, nightWindow.Start.Trim()));
            }

            if (!hasEnd && !string.IsNullOrWhiteSpace(nightWindow.End))
            {
                RegionSetupFieldParser.ValidateTimeOfDay(nightWindow.End, "surcharges.nightWindow.end");
                toWrite.Add((SettingKeys.SurchargeNightEnd, nightWindow.End.Trim()));
            }

            if (toWrite.Count == 0)
            {
                return;
            }

            var changedSurchargeSettingKeys = await _changeDetector.ComputeChangedSurchargeRelevantKeysAsync(toWrite);

            await WriteSettingsInTransactionAsync(toWrite);

            await _eventPublisher.DispatchSurchargeSettingsChangedAsync(changedSurchargeSettingKeys);

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
    public async Task BackfillOvertimeIfPresentAsync(string filePath)
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
                toWrite.Add((SettingKeys.SurchargeStackingMode, RegionSetupFieldParser.ValidateStackingMode(profile.Surcharges!.StackingMode!, "surcharges.stackingMode")));
            }

            if (!hasAnyTierRate && profile.Surcharges?.Overtime != null)
            {
                var overtime = profile.Surcharges.Overtime;
                if (!string.IsNullOrWhiteSpace(overtime.Basis))
                {
                    toWrite.Add((SettingKeys.OvertimeBasis, RegionSetupFieldParser.ValidateOvertimeBasis(overtime.Basis, "surcharges.overtime.basis")));
                }

                if (!string.IsNullOrWhiteSpace(overtime.RateMode))
                {
                    toWrite.Add((SettingKeys.OvertimeRateMode, RegionSetupFieldParser.ValidateOvertimeRateMode(overtime.RateMode, "surcharges.overtime.rateMode")));
                }

                RegionSetupSurchargeSettingsBuilder.AddOvertimeTierSettings(overtime.Tiers, toWrite);
            }

            if (toWrite.Count == 0)
            {
                return;
            }

            var changedSurchargeSettingKeys = await _changeDetector.ComputeChangedSurchargeRelevantKeysAsync(toWrite);

            await WriteSettingsInTransactionAsync(toWrite);

            await _eventPublisher.DispatchSurchargeSettingsChangedAsync(changedSurchargeSettingKeys);

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
    public async Task BackfillCompensatoryRestIfPresentAsync(string filePath)
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
            RegionSetupComplianceSettingsBuilder.AddCompensatoryRestSettings(compensatoryRest, toWrite);
            if (toWrite.Count == 0)
            {
                return;
            }

            await WriteSettingsInTransactionAsync(toWrite);

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
    public async Task ApplyPackageIdentityIfPresentAsync(string filePath)
    {
        try
        {
            var profile = await RegionSetupFileReader.ReadProfileAsync(filePath);
            if (profile.Package == null)
            {
                return;
            }

            var toWrite = new List<(string Type, string Value)>();
            RegionSetupSettingsBuilder.AddPackageIdentitySettings(profile.Package, toWrite);

            var changed = await _changeDetector.FindChangedSettingsAsync(toWrite);
            if (changed.Count == 0)
            {
                return;
            }

            await WriteSettingsInTransactionAsync(changed);

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

    private async Task WriteSettingsInTransactionAsync(IReadOnlyList<(string Type, string Value)> toWrite)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var (type, value) in toWrite)
            {
                await _settingsRepository.UpsertSettingAsync(type, value);
            }

            await _unitOfWork.CompleteAsync();
            return toWrite.Count;
        });
    }

    private async Task<bool> HasAnyOvertimeTierRateAsync()
    {
        foreach (var key in RegionSetupSurchargeSettingsBuilder.OvertimeTierRateKeys)
        {
            if (await _settingsRepository.GetSetting(key) != null)
            {
                return true;
            }
        }

        return false;
    }
}
