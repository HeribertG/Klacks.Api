// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Result of asking the active turn registry to stop a turn. Deliberately two-valued: a turn that does not
/// exist, has already finished or belongs to another user all answer NotFound, so the answer is never an
/// oracle for the existence of somebody else's turn.
/// </summary>
public enum StopRequestOutcome
{
    NotFound,
    Accepted
}
