// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Interfaces;

/// <summary>
/// An ONNX inference session that can hand its native memory back to the process while idle and
/// rebuild itself transparently on the next call.
/// </summary>
public interface IUnloadableInferenceSession
{
    /// <summary>Stable name for log lines; the type name is an implementation detail.</summary>
    string SessionName { get; }

    /// <summary>False while no session is held, so the sweep can skip the call entirely.</summary>
    bool IsLoaded { get; }

    /// <summary>How often the session has been built in this process; a rising number under a
    /// steady request pattern is the thrashing signal.</summary>
    int LoadCount { get; }

    /// <summary>Releases the session if - and only if - nothing is running on it and its last use is
    /// older than <paramref name="idleFor"/>. Never blocks: if the session is busy or being built,
    /// this returns false and the caller retries on its next tick.</summary>
    /// <param name="idleFor">How long the session must have sat unused before it may be released.</param>
    /// <param name="cancellationToken">Cancels the attempt to take the provider's init lock.</param>
    Task<bool> TryUnloadIfIdleAsync(TimeSpan idleFor, CancellationToken cancellationToken);
}
