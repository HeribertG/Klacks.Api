// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

/// <summary>
/// Model-facing texts of a turn the user stopped. English on purpose: they are read by the language model,
/// never shown to the user, who is told in his own language by the client.
/// </summary>
public static class TurnInterruptionDefaults
{
    /// <summary>Result of a call that did not run because the user stopped the turn.</summary>
    public const string SkippedCallResult = "[Not executed: the user stopped this turn before the call ran.]";
}
