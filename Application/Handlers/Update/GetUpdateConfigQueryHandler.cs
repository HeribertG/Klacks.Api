// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the admin-editable auto-update behaviour configuration from the generic settings store,
/// applying defaults for any unset key.
/// </summary>
using SettingsConstants = Klacks.Api.Application.Constants.Settings;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Queries.Update;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Update;

public class GetUpdateConfigQueryHandler : IRequestHandler<GetUpdateConfigQuery, UpdateConfig>
{
    private readonly ISettingsReader _settingsReader;

    public GetUpdateConfigQueryHandler(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<UpdateConfig> Handle(GetUpdateConfigQuery request, CancellationToken cancellationToken)
    {
        var config = new UpdateConfig();

        config.AutoEnabled = bool.TryParse(await ReadAsync(SettingsConstants.UPDATE_AUTO_ENABLED), out var auto) && auto;
        config.Channel = await ReadAsync(SettingsConstants.UPDATE_CHANNEL) is { Length: > 0 } channel ? channel : config.Channel;
        config.CheckIntervalHours = int.TryParse(await ReadAsync(SettingsConstants.UPDATE_CHECK_INTERVAL_HOURS), out var interval) ? interval : config.CheckIntervalHours;
        config.MaintenanceWindowStart = await ReadAsync(SettingsConstants.UPDATE_MAINTENANCE_WINDOW_START) ?? string.Empty;
        config.MaintenanceWindowEnd = await ReadAsync(SettingsConstants.UPDATE_MAINTENANCE_WINDOW_END) ?? string.Empty;
        config.NotifyOnly = bool.TryParse(await ReadAsync(SettingsConstants.UPDATE_NOTIFY_ONLY), out var notify) && notify;
        config.BackupRetentionCount = int.TryParse(await ReadAsync(SettingsConstants.UPDATE_BACKUP_RETENTION_COUNT), out var retention) ? retention : config.BackupRetentionCount;
        config.PinnedVersion = await ReadAsync(SettingsConstants.UPDATE_PINNED_VERSION) ?? string.Empty;

        return config;
    }

    private async Task<string?> ReadAsync(string type)
    {
        var setting = await _settingsReader.GetSetting(type);
        return setting?.Value;
    }
}
