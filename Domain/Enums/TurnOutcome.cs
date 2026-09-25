// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// How a streamed chat turn ended, claimed exactly once on the turn's run state. A turn that ends without
/// any of these was left mid-way (dropped connection, unexpected exception) and is picked up by the
/// interrupted-turn safety net.
/// </summary>
public enum TurnOutcome
{
    /// <summary>The turn ran to its regular end and persisted itself.</summary>
    Completed = 0,

    /// <summary>The user asked for a stop and the turn ended at a safe point.</summary>
    Stopped = 1,

    /// <summary>The turn answered with a clarification question and persisted itself.</summary>
    Clarified = 2,

    /// <summary>The turn ended on an error and told the client so.</summary>
    Errored = 3
}
