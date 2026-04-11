// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Factory abstraction for Speech-to-Text providers.
/// Each call to CreateSessionAsync returns an independent ISttSession for a single connection.
/// </summary>
/// <param name="ProviderId">Unique provider identifier (e.g. "deepgram", "groq-whisper")</param>
namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface ISttProvider
{
    string ProviderId { get; }
    Task<ISttSession> CreateSessionAsync(SttConfig config, CancellationToken ct = default);
}
