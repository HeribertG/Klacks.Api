// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Represents an available TTS voice for user selection.
/// </summary>
/// <param name="VoiceId">Unique voice identifier used when synthesizing</param>
/// <param name="Locale">BCP-47 locale key (e.g. "de", "en")</param>
/// <param name="DisplayName">Human-readable voice name</param>
namespace Klacks.Api.Domain.Models.Assistant;

public record TtsVoice(string VoiceId, string Locale, string DisplayName);
