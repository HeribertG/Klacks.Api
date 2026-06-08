// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the ElevenLabs text-to-speech provider (endpoint template, model, voices and limits).
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class ElevenLabsTtsConstants
{
    public const string ApiUrlTemplate = "https://api.elevenlabs.io/v1/text-to-speech/{0}";
    public const string Model = "eleven_multilingual_v2";
    public const string ApiKeyHeader = "xi-api-key";
    public const string LlmProviderId = "elevenlabs";
    public const string DefaultVoiceId = "21m00Tcm4TlvDq8ikWAM";
    public const int MaxInputLength = 5000;

    public static readonly IReadOnlyDictionary<string, string> Voices = new Dictionary<string, string>
    {
        ["21m00Tcm4TlvDq8ikWAM"] = "Rachel",
        ["AZnzlk1XvdvUeBnXmlld"] = "Domi",
        ["EXAVITQu4vr4xnSDxMaL"] = "Sarah",
        ["ErXwobaYiN019PkySvjV"] = "Antoni",
        ["pNInz6obpgDQGcFmaJgB"] = "Adam",
        ["yoZ06aMxZJJ28mfd3POQ"] = "Sam",
    };
}
