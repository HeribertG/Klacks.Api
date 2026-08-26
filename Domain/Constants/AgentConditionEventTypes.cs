// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The AgentConditionEvent.EventType values that are NOT status transitions. Transitions spell their
/// event type as the target AgentConditionStatus and need no constant; these are the operational
/// events the action dispatcher appends while a row's status stays where it is.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class AgentConditionEventTypes
{
    /// <summary>A remediation attempt ran and failed; the row stays Prepared and may be retried.</summary>
    public const string AttemptFailed = "AttemptFailed";

    /// <summary>An abandoned Prepared claim was taken over after the stale-claim window elapsed.</summary>
    public const string Reclaimed = "Reclaimed";
}
