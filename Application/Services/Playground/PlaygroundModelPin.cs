// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the pinned Playground models from the configured options when Deployment:IsPlayground is true and
/// passes the caller's choice through unchanged otherwise.
/// </summary>
/// <param name="mode">Tells whether the instance is the Playground</param>
/// <param name="options">The pinned chat and transcription models</param>

using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Models.Playground;
using Klacks.Api.Domain.Services.Playground;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Services.Playground;

public class PlaygroundModelPin : IPlaygroundModelPin
{
    private readonly IPlaygroundMode _mode;
    private readonly PlaygroundModelOptions _options;

    public PlaygroundModelPin(IPlaygroundMode mode, IOptions<PlaygroundModelOptions> options)
    {
        _mode = mode;
        _options = options.Value;
    }

    public bool IsPinned => _mode.IsActive;

    public string? ResolveChatModelId(string? requestedModelId) =>
        _mode.IsActive ? _options.ChatModelId : requestedModelId;

    public PlaygroundTranscriptionTarget? GetTranscriptionTarget() =>
        _mode.IsActive
            ? new PlaygroundTranscriptionTarget(_options.TranscriptionProviderId, _options.TranscriptionApiModelId)
            : null;
}
