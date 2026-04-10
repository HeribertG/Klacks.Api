// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Constants for STT provider configuration and identification.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class SttProviderConstants
{
    public const string Deepgram = "deepgram";
    public const string GroqWhisper = "groq-whisper";
    public const string AssemblyAi = "assemblyai";
    public const string Browser = "browser";

    public const string DeepgramWssUrl = "wss://api.deepgram.com/v1/listen";
    public const string AssemblyAiWssUrl = "wss://api.assemblyai.com/v2/realtime/ws";
    public const string GroqWhisperRestUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
}
