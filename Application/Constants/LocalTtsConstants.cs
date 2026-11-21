// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants of the local text-to-speech provider: the Supertonic 3 model installed into Klacks.Stt by the updater, its
/// ten voices, the licence version the administrator consents to before the download, and the HTTP contract of the
/// Klacks.Stt speech endpoint. Model alias and model id must match Klacks.Updater TtsPluginConstants.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class LocalTtsConstants
{
    public const string ModelAlias = "supertonic-3";
    public const string ModelId = "supertonic-3-int8";
    public const long ModelSizeBytes = 128_774_318;

    public const string LicenseVersion = "openrail-m-supertonic3-3cadd1ee";
    public const string LicenseResourceName = "Klacks.Api.LocalTts.OpenRailMSupertonic3.txt";

    public const string SpeechPath = "/v1/audio/speech";
    public const string HttpClientName = "LocalTts";
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromMinutes(5);

    // Klacks.Stt rejects longer inputs with 413 text_too_long; longer texts are split at sentence boundaries.
    public const int MaxRequestTextLength = 4000;
    public const int LanguageCodeLength = 2;

    public const string ErrorUnsupportedLanguage = "unsupported_language";
    public const string ErrorModelNotInstalled = "model_not_installed";

    public const string VoicePrefix = "supertonic:";
    public const string DefaultVoiceId = VoicePrefix + "F1";

    public static readonly IReadOnlyList<string> VoiceNames =
        ["F1", "F2", "F3", "F4", "F5", "M1", "M2", "M3", "M4", "M5"];

    public static readonly IReadOnlyList<string> VoiceIds = VoiceNames.Select(name => VoicePrefix + name).ToList();

    public const string NotInstalledMessage =
        "The local voice is not installed or not reachable. Install it in the speech settings.";
}
