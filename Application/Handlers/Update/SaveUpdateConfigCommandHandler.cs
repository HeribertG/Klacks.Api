// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persists the admin-editable auto-update behaviour configuration by upserting each UPDATE_* key in
/// the generic settings store. The trust root (manifest URL, signature key) is intentionally NOT here.
/// </summary>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using System.Globalization;
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using SettingsModel = Klacks.Api.Domain.Models.Settings.Settings;

namespace Klacks.Api.Application.Handlers.Update;

public class SaveUpdateConfigCommandHandler : IRequestHandler<SaveUpdateConfigCommand, UpdateConfig>
{
    private readonly ISettingsRepository _settingsRepository;

    public SaveUpdateConfigCommandHandler(ISettingsRepository settingsRepository)
    {
        _settingsRepository = settingsRepository;
    }

    public async Task<UpdateConfig> Handle(SaveUpdateConfigCommand request, CancellationToken cancellationToken)
    {
        var config = request.Config;
        var values = new Dictionary<string, string>
        {
            [SettingsConstants.UPDATE_AUTO_ENABLED] = config.AutoEnabled.ToString(),
            [SettingsConstants.UPDATE_CHANNEL] = config.Channel,
            [SettingsConstants.UPDATE_CHECK_INTERVAL_HOURS] = config.CheckIntervalHours.ToString(CultureInfo.InvariantCulture),
            [SettingsConstants.UPDATE_MAINTENANCE_WINDOW_START] = config.MaintenanceWindowStart,
            [SettingsConstants.UPDATE_MAINTENANCE_WINDOW_END] = config.MaintenanceWindowEnd,
            [SettingsConstants.UPDATE_NOTIFY_ONLY] = config.NotifyOnly.ToString(),
            [SettingsConstants.UPDATE_BACKUP_RETENTION_COUNT] = config.BackupRetentionCount.ToString(CultureInfo.InvariantCulture),
            [SettingsConstants.UPDATE_PINNED_VERSION] = config.PinnedVersion,
        };

        foreach (var (type, value) in values)
        {
            await UpsertAsync(type, value ?? string.Empty);
        }

        return config;
    }

    private async Task UpsertAsync(string type, string value)
    {
        var existing = await _settingsRepository.GetSetting(type);
        if (existing is null)
        {
            await _settingsRepository.AddSetting(new SettingsModel { Id = Guid.NewGuid(), Type = type, Value = value });
        }
        else
        {
            existing.Value = value;
            await _settingsRepository.PutSetting(existing);
        }
    }
}
