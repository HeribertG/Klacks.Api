// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Defaults for the per-conversation "recently created/updated entity" memory that lets the
/// assistant resolve follow-up references ("delete it again", "edit the one I just created")
/// after the original mention has scrolled out of the compacted chat history.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class RecentEntityDefaults
{
    /// <summary>
    /// Ring size: the maximum number of recent-entity rows retained per (user, conversation).
    /// Older rows are hard-deleted once this bound is exceeded.
    /// </summary>
    public const int MaxPerConversation = 10;

    public const string ActionCreated = "created";

    public const string ActionUpdated = "updated";
}
