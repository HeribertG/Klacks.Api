// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.DTOs.Filter;

/// <summary>
/// Narrows the set of open orders (Shift rows still in status OriginalOrder) a bulk operation works on.
/// Scenario rows and soft-deleted rows are excluded by the repository itself, not by this filter.
/// </summary>
/// <param name="SourceSystemId">Only orders imported from this external system; null for every source.</param>
/// <param name="FromDate">Only orders starting on or after this date; null for no lower bound.</param>
/// <param name="UntilDate">Only orders starting on or before this date; null for no upper bound.</param>
/// <param name="CustomerName">Case-insensitive fragment the customer's name or company must contain; null for every customer.</param>
/// <param name="GroupId">Only orders already linked to this group; null ignores group membership.</param>
/// <param name="MaxCount">Upper bound on the number of orders returned; null returns all matches.</param>
public sealed record OpenOrderFilter(
    string? SourceSystemId,
    DateOnly? FromDate,
    DateOnly? UntilDate,
    string? CustomerName,
    Guid? GroupId,
    int? MaxCount);
