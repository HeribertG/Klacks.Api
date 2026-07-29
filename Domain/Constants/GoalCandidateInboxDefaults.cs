// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Paging defaults for the goal candidate inbox (Phase 2), shared between the list query handler
/// and the REST endpoint so a missing or excessive take parameter is normalized consistently.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class GoalCandidateInboxDefaults
{
    public const int DefaultListTake = 50;

    public const int MaxListTake = 200;
}
