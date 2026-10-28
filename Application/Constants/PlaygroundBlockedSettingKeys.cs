// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Constants;

/// <summary>
/// Setting keys that must not be written on the public Playground: the DeepL key, every STT and TTS provider key and
/// the STT engine / TTS provider choice, plus the update channel and the pinned update version (the demo admin must not
/// steer which release an updater would install). Voice, output mode, thresholds, transcription model and all other settings
/// are deliberately not listed because the UI and the app write them in normal operation.
/// </summary>
public static class PlaygroundBlockedSettingKeys
{
    public static readonly IReadOnlySet<string> Keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        Settings.DEEPL_API_KEY,
        Settings.ASSISTANT_STT_ENGINE,
        Settings.ASSISTANT_STT_API_KEY,
        Settings.ASSISTANT_STT_API_KEY_DEEPGRAM,
        Settings.ASSISTANT_STT_API_KEY_GROQ,
        Settings.ASSISTANT_STT_API_KEY_ASSEMBLYAI,
        Settings.ASSISTANT_TTS_PROVIDER,
        Settings.ASSISTANT_TTS_API_KEY_OPENAI,
        Settings.ASSISTANT_TTS_API_KEY_ELEVENLABS,
        Settings.ASSISTANT_TTS_API_KEY_GOOGLE,
        Settings.UPDATE_PINNED_VERSION,
        Settings.UPDATE_CHANNEL,
    };

    public static bool IsBlocked(string? settingKey) => !string.IsNullOrEmpty(settingKey) && Keys.Contains(settingKey);
}
