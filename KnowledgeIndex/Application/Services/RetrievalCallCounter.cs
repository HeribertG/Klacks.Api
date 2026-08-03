// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Counts how often the retrieval pipeline runs inside a single request. Registered scoped, so the
/// count is per turn.
/// A turn can trigger more than one full pass and none of them was visible before: the toolset
/// assembler always runs one, and the recipe engine's semantic fallback runs another with a different
/// query whenever no keyword trigger matched. Since the cross-encoder dominates retrieval latency,
/// the number of passes matters as much as the duration of one - and it cannot be read from a log
/// that reports only durations.
/// </summary>
public sealed class RetrievalCallCounter
{
    private int _calls;

    /// <summary>
    /// Returns the ordinal of this pass within the current request, starting at 1.
    /// </summary>
    public int NextCall() => ++_calls;
}
