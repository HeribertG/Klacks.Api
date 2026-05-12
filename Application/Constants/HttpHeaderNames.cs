// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Constants;

public static class HttpHeaderNames
{
    public const string SignalRConnectionId = "X-SignalR-ConnectionId";

    /// <summary>
    /// Identifies the group the calling client is currently viewing. Used by
    /// CRUD handlers that build refresh payloads (Break, Work, WorkChange) to
    /// pass the correct visible-group-id list to GetScheduleEntries so the
    /// returned entries keep their is_group_restricted (sealed) flag.
    /// </summary>
    public const string SelectedGroup = "X-Selected-Group";
}
