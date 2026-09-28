// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Abstraction for Text-to-Speech providers that synthesize audio from text.
/// </summary>
/// <param name="ProviderId">Unique provider identifier (e.g. "edge", "openai")</param>
namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface ITtsProvider
{
    string ProviderId { get; }
    Task<byte[]> SynthesizeAsync(string text, string voiceId, string locale, CancellationToken ct = default);
    Task<IReadOnlyList<TtsVoice>> GetVoicesAsync(CancellationToken ct = default);
}
