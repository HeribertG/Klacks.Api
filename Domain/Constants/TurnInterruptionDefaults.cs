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

    /// <summary>Appended to the stored answer of a stopped or interrupted turn, so the model reads later that the answer above it is cut off.</summary>
    public const string InterruptedMarker = "[interrupted by user]";

    /// <summary>Appended to the stored answer of a turn that ended on an error after write actions had run. Neutral on purpose: the user did not stop it, and the model must not tell him he did.</summary>
    public const string ErroredMarker = "[interrupted by an error]";

    /// <summary>Error text of the usage row of a turn that ended on an error after write actions had run.</summary>
    public const string ErroredUsageMessage = "The turn ended on an error after write actions had run.";
}
