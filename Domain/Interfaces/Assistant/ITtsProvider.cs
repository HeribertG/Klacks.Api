// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Abstraction for Text-to-Speech providers that synthesize audio from text.
/// </summary>
/// <param name="ProviderId">Unique provider identifier (e.g. "edge", "openai")</param>
/// <param name="MediaType">Media type of the synthesized audio (e.g. "audio/mpeg", "audio/wav")</param>
/// <param name="ChunksInternally">True when the provider splits long texts itself and returns one playable file; false
/// when the caller splits the text and concatenates the returned pieces byte by byte</param>
namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface ITtsProvider
{
    string ProviderId { get; }
    string MediaType { get; }
    bool ChunksInternally { get; }
    Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default);
    Task<IReadOnlyList<TtsVoice>> GetVoicesAsync(CancellationToken ct = default);
}
