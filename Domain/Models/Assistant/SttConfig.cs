// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Configuration for establishing an STT provider connection.
/// </summary>
/// <param name="ApiKey">Provider API key</param>
/// <param name="Language">BCP-47 language code (e.g. "de", "en")</param>
/// <param name="SampleRate">Audio sample rate in Hz (default 16000)</param>
namespace Klacks.Api.Domain.Models.Assistant;

public record SttConfig(string ApiKey, string Language, int SampleRate = 16000);
