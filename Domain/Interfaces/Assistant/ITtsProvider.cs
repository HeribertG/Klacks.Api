// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Abstraction for Text-to-Speech providers that synthesize audio from text.
/// </summary>
/// <param name="ProviderId">Unique provider identifier (e.g. "edge", "openai")</param>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ITtsProvider
{
    string ProviderId { get; }
    Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default);
}
