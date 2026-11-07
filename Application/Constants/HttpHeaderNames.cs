// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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

    /// <summary>
    /// Base64 RSA-SHA256 vendor signature over the exact response bytes of a Klacks Marketplace package
    /// download (region package, language pack, feature plugin).
    /// </summary>
    public const string KlacksSignature = "X-Klacks-Signature";

    /// <summary>
    /// Client-chosen GUID that marks one user action on a creating endpoint, so a repeated request of the same action
    /// (automatic retry after a lost answer) is answered from the first execution instead of creating a second entry.
    /// </summary>
    public const string IdempotencyKey = "Idempotency-Key";
}
