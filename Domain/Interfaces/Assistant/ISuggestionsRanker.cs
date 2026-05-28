// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Ranks Klacksy welcome suggestion i18n keys from four signals: user's skill-usage history,
/// current frontend route, user roles, and local time (weekday/hour). Returns the top-N keys.
/// </summary>
public interface ISuggestionsRanker
{
    Task<IReadOnlyList<string>> RankAsync(
        Guid userId,
        IReadOnlyList<string> userRights,
        string? currentRoute,
        int localHour,
        int weekday,
        int topN,
        CancellationToken cancellationToken = default);
}
