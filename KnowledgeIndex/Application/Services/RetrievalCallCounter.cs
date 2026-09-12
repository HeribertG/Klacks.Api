// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Counts how often the retrieval pipeline runs inside a single DI scope.
/// A turn can trigger more than one full pass: the toolset assembler always runs one, and the recipe
/// engine's semantic fallback runs another with a different query whenever no keyword trigger matched.
/// Since the cross-encoder dominates retrieval latency, the number of passes matters as much as the
/// duration of one.
/// The ordinal is per scope, not per turn: the recipe engine resolves inside a child scope, which gets
/// its own instance and starts at 1 again. Counting the passes of a turn therefore means counting log
/// lines that carry the same TurnCorrelation id, not reading this ordinal.
/// </summary>
public sealed class RetrievalCallCounter
{
    private int _calls;

    /// <summary>
    /// Returns the ordinal of this pass within the current DI scope, starting at 1.
    /// </summary>
    public int NextCall() => ++_calls;
}
