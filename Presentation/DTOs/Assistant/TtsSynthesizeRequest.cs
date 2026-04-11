// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request to synthesize text into speech audio.
/// </summary>
/// <param name="Text">The text content to convert to speech</param>
/// <param name="Locale">The locale code for voice selection (e.g. "de", "en")</param>
/// <param name="ProviderId">TTS provider identifier; defaults to "edge" when null</param>
/// <param name="VoiceId">Explicit voice identifier; pass "auto" or null for locale-based selection</param>
namespace Klacks.Api.Presentation.DTOs.Assistant;

public record TtsSynthesizeRequest(string Text, string? Locale, string? ProviderId = null, string? VoiceId = null);
