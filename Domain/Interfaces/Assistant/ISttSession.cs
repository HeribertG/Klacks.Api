// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Represents an active STT session with per-connection state.
/// Obtained from ISttProvider.CreateSessionAsync and must be disposed after use.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

using Klacks.Api.Domain.Models.Assistant;

public interface ISttSession : IAsyncDisposable
{
    Task SendAudioAsync(byte[] audioChunk, CancellationToken ct = default);
    Task<SttResult?> ReceiveAsync(CancellationToken ct = default);
}
