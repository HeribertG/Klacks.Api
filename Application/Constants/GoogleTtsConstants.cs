// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the Google Cloud text-to-speech provider (endpoint, encoding, voices and limits).
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class GoogleTtsConstants
{
    public const string ApiUrl = "https://texttospeech.googleapis.com/v1/text:synthesize";
    public const string AudioEncoding = "MP3";
    public const string LlmProviderId = "google-tts";
    public const string DefaultVoice = "en-US-Neural2-D";
    public const string DefaultLanguageCode = "en-US";
    public const int MaxInputLength = 5000;

    public static readonly IReadOnlyDictionary<string, string> Voices = new Dictionary<string, string>
    {
        ["de-DE-Neural2-B"] = "German (de-DE) - Male",
        ["de-DE-Neural2-C"] = "German (de-DE) - Female",
        ["en-US-Neural2-D"] = "English (en-US) - Male",
        ["en-US-Neural2-F"] = "English (en-US) - Female",
        ["fr-FR-Neural2-B"] = "French (fr-FR) - Male",
        ["it-IT-Neural2-C"] = "Italian (it-IT) - Female",
        ["es-ES-Neural2-B"] = "Spanish (es-ES) - Male",
    };

    public static readonly IReadOnlyDictionary<string, string> LocaleDefaults = new Dictionary<string, string>
    {
        ["de"] = "de-DE-Neural2-B",
        ["en"] = "en-US-Neural2-D",
        ["fr"] = "fr-FR-Neural2-B",
        ["it"] = "it-IT-Neural2-C",
        ["es"] = "es-ES-Neural2-B",
    };
}
