// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Setting keys that only the server writes, never a client through the generic settings endpoints (AddSetting,
/// PutSetting), not even an administrator: the Klacks.Stt address of the local voice, written by the updater after the
/// installation, and the three keys of the licence consent, written by the local voice install handler. A client value
/// could forge the consent record or point the server-side speech request at another host.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ServerManagedSettingKeys
{
    public static readonly IReadOnlySet<string> Keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Settings.ASSISTANT_TTS_LOCAL_BASE_URL,
        Settings.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_BY,
        Settings.ASSISTANT_TTS_LOCAL_LICENSE_ACCEPTED_AT,
        Settings.ASSISTANT_TTS_LOCAL_LICENSE_VERSION,
    };

    public const string WriteRejectedMessage = "This setting is managed by the server and cannot be changed through the settings endpoints.";

    public static bool IsServerManaged(string? settingKey) => !string.IsNullOrWhiteSpace(settingKey) && Keys.Contains(settingKey.Trim());
}
