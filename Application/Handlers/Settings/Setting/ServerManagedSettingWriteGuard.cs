// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Rejects a write through the generic settings commands when it targets a server-managed key
/// (ServerManagedSettingKeys), whether the key is named in the body or is the key of the stored row the body's id
/// addresses, so a write cannot hide the key behind another Type. Rejection is a ForbiddenException (HTTP 403), for every
/// caller including administrators.
/// </summary>
/// <param name="settingsRepository">Reads the stored row of a setting by its id</param>
/// <param name="setting">The setting a client wants to write</param>
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using SettingEntity = Klacks.Api.Domain.Models.Settings.Settings;

namespace Klacks.Api.Application.Handlers.Settings.Setting;

public static class ServerManagedSettingWriteGuard
{
    public static async Task EnsureWritableAsync(ISettingsRepository settingsRepository, SettingEntity setting)
    {
        if (ServerManagedSettingKeys.IsServerManaged(setting.Type))
        {
            throw new ForbiddenException(ServerManagedSettingKeys.WriteRejectedMessage);
        }

        if (setting.Id == Guid.Empty)
        {
            return;
        }

        var stored = await settingsRepository.GetSettingByIdNoTracking(setting.Id);
        if (stored is not null && ServerManagedSettingKeys.IsServerManaged(stored.Type))
        {
            throw new ForbiddenException(ServerManagedSettingKeys.WriteRejectedMessage);
        }
    }
}
