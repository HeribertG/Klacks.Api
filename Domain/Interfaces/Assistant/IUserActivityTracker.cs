// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Tracks the last time a user actively interacted with the chat, so proactive triggers can avoid
/// interrupting an ongoing conversation. In-memory; resets on restart (deliberately — dedup handles
/// persistence so a restart never re-floods).
/// </summary>
public interface IUserActivityTracker
{
    void MarkActive(string userId);

    bool IsRecentlyActive(string userId, TimeSpan window);
}
