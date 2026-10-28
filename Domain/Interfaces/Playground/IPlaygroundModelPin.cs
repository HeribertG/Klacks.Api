// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Playground;

namespace Klacks.Api.Domain.Interfaces.Playground;

/// <summary>
/// Pins the models of the public Playground. Without the Playground mode both members hand the caller's choice
/// back untouched, so customer installations resolve models exactly as before.
/// </summary>
public interface IPlaygroundModelPin
{
    /// <summary>
    /// True when the instance is the Playground and the model is therefore pinned.
    /// </summary>
    bool IsPinned { get; }

    /// <summary>
    /// The chat model to use: the pinned model on the Playground (the client's choice is ignored), otherwise the requested one.
    /// </summary>
    /// <param name="requestedModelId">Model id the client sent, null or blank when it sent none</param>
    string? ResolveChatModelId(string? requestedModelId);

    /// <summary>
    /// The pinned transcription provider and model on the Playground, null otherwise.
    /// </summary>
    PlaygroundTranscriptionTarget? GetTranscriptionTarget();
}
