// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service interface for text-to-speech synthesis.
/// </summary>
/// <param name="text">The text to synthesize into speech</param>
/// <param name="locale">The locale code determining which voice to use (e.g., "de", "en")</param>
/// <param name="voiceId">Explicit voice identifier; pass "auto" or null to use locale-based auto-selection</param>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITextToSpeechService
{
    Task<byte[]> SynthesizeAsync(string text, string locale, CancellationToken ct = default);
    Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default);
}
