// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Result of a turn-options lookup. Never serialized to the wire: the controller answers the bare option
/// list either way. The lookup is scoped to the caller, so a turn of another user is not a separate
/// outcome - it is simply not found, and the browser learns nothing about somebody else's turn.
/// </summary>
public enum TurnOptionsOutcome
{
    Found = 1,
    NotFound = 2
}
