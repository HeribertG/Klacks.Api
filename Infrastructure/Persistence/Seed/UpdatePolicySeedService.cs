// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Seeds the initial auto-update policy of a customer installation at startup. Writes only the settings the
/// installer configured (see <see cref="UpdateInitialPolicyOptions"/>) and only where no row exists yet, so a value
/// saved in the admin UI is never overwritten and an installation without these keys (vendor production,
/// development, integration tests) never touches the database here.
/// </summary>
/// <param name="options">Initial policy configured by the customer installer; unset or invalid values are not seeded</param>
/// <param name="settingsRepository">Reads the existing update settings and adds the missing ones</param>
/// <param name="unitOfWork">Persists the seeded settings</param>
/// <param name="logger">Logger for diagnostic output</param>

using Klacks.Api.Application.Configuration;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Microsoft.Extensions.Options;
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using SettingsModel = Klacks.Api.Domain.Models.Settings.Settings;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class UpdatePolicySeedService
{
    private readonly UpdateInitialPolicyOptions _options;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePolicySeedService> _logger;

    public UpdatePolicySeedService(
        IOptions<UpdateInitialPolicyOptions> options,
        ISettingsRepository settingsRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePolicySeedService> logger)
    {
        _options = options.Value;
        _settingsRepository = settingsRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <returns>Number of settings written; 0 when nothing is configured or every setting already exists</returns>
    public async Task<int> SeedAsync()
    {
        var planned = BuildPlannedSettings();
        if (planned.Count == 0)
        {
            return 0;
        }

        var existing = await _settingsRepository.GetSettingsByTypesAsync(planned.Keys);
        var missing = planned.Where(entry => !existing.ContainsKey(entry.Key)).ToList();
        if (missing.Count == 0)
        {
            return 0;
        }

        foreach (var (type, value) in missing)
        {
            await _settingsRepository.AddSetting(new SettingsModel { Id = Guid.NewGuid(), Type = type, Value = value });
        }

        try
        {
            await _unitOfWork.CompleteAsync();
        }
        catch (DatabaseUpdateException ex) when (ex.IsDuplicate)
        {
            _logger.LogWarning("Update policy seed skipped: another instance wrote the update settings concurrently.");
            return 0;
        }

        foreach (var (type, value) in missing)
        {
            _logger.LogInformation("Update policy seed: {Setting} initialised to {Value} for this installation.", type, value);
        }

        return missing.Count;
    }

    private Dictionary<string, string> BuildPlannedSettings()
    {
        var planned = new Dictionary<string, string>(StringComparer.Ordinal);
        AddIfValid(planned, SettingsConstants.UPDATE_AUTO_ENABLED, _options.InitialAutoEnabled, nameof(UpdateInitialPolicyOptions.InitialAutoEnabled));
        AddIfValid(planned, SettingsConstants.UPDATE_NOTIFY_ONLY, _options.InitialNotifyOnly, nameof(UpdateInitialPolicyOptions.InitialNotifyOnly));
        return planned;
    }

    private void AddIfValid(Dictionary<string, string> planned, string settingType, string? configured, string optionName)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            return;
        }

        if (!bool.TryParse(configured.Trim(), out var value))
        {
            _logger.LogWarning(
                "Update policy seed: {Section}:{Option} has the invalid value '{Value}' (expected true or false); {Setting} is not seeded.",
                UpdateInitialPolicyOptions.SectionName, optionName, configured, settingType);
            return;
        }

        planned[settingType] = value.ToString();
    }
}
