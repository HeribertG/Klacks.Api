// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Constants for TTS provider configuration and identification.
/// </summary>
namespace Klacks.Api.Application.Constants;

public static class TtsProviderConstants
{
    public const string Edge = "edge";
    public const string OpenAi = "openai";
    public const string ElevenLabs = "elevenlabs";
    public const string Google = "google-tts";
    public const string Local = "local";
    public const string AutoVoice = "auto";
    public const string DefaultLocale = "en";
    public const string MpegMediaType = "audio/mpeg";
    public const string WavMediaType = "audio/wav";

    // Long texts are split into sentence-boundary chunks of this size and synthesized
    // sequentially; 2500 stays below every provider's per-request limit (OpenAI 4096
    // chars, Google 5000 bytes, Edge 5000 chars).
    public const int SynthesisChunkLength = 2500;
    public const int MaxSynthesisTextLength = 24000;
}
