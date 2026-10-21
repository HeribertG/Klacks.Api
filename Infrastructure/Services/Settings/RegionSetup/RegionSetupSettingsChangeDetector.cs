// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Compares planned region-setup setting writes against the stored values BEFORE the write, so a
/// re-import of unchanged values neither writes nor triggers a recalculation.
/// </summary>
/// <param name="settingsRepository">Reads the stored setting values untracked</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.Settings.RegionSetup;

public sealed class RegionSetupSettingsChangeDetector
{
    private readonly ISettingsRepository _settingsRepository;

    public RegionSetupSettingsChangeDetector(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<List<(string Type, string Value)>> FindChangedSettingsAsync(
        IReadOnlyList<(string Type, string Value)> plannedSettings)
    {
        var changed = new List<(string Type, string Value)>();
        foreach (var (type, value) in plannedSettings)
        {
            var currentValue = (await _settingsRepository.GetSettingNoTracking(type))?.Value;
            if (!string.Equals(currentValue, value, StringComparison.Ordinal))
            {
                changed.Add((type, value));
            }
        }

        return changed;
    }

    // Compares each planned surcharge-relevant setting against its stored value BEFORE the write.
    // The comparison must happen pre-write: UpsertSettingAsync mutates the tracked Settings entity,
    // so reading it after the write always reports "unchanged". Computed once outside the transaction
    // lambda, so an EF execution-strategy retry cannot skew the result either.
    public async Task<List<string>> ComputeChangedSurchargeRelevantKeysAsync(
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
}
