// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

/// <summary>
/// Rebuilds the ONNX inference sessions in the background when a question is about to come, so the user
/// does not pay for a cold start after the idle sweep released them.
/// </summary>
public interface IInferenceSessionPrewarmer
{
    /// <summary>
    /// Starts rebuilding the sessions without waiting for it. Does nothing when every session is already
    /// loaded, when none exist (no ONNX on this host) or when a rebuild is already running.
    /// </summary>
    /// <returns>True when this call started a rebuild</returns>
    bool RequestPrewarm();
}
