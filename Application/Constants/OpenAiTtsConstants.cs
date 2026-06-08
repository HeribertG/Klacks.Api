// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for the OpenAI text-to-speech provider (endpoint, model, voices and limits).
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class OpenAiTtsConstants
{
    public const string ApiUrl = "https://api.openai.com/v1/audio/speech";
    public const string Model = "gpt-4o-mini-tts";
    public const string ResponseFormat = "mp3";
    public const string DefaultVoice = "alloy";
    public const int MaxInputLength = 4096;
    public const string LlmProviderId = "openai";

    public static readonly IReadOnlyList<string> Voices = new[]
    {
        "alloy", "ash", "ballad", "coral", "echo", "fable", "onyx", "nova", "sage", "shimmer", "verse"
    };
}
