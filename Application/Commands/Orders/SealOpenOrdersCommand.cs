// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Orders;

/// <summary>
/// Seals every open order (status OriginalOrder) matching the filter, one transaction per order, so a
/// single failure never rolls back the rest of the batch. With Apply=false it only reports how many are
/// sealable and what blocks the others. With AutoAssignGroups=true the group assignment runs first, so
/// orders blocked only by a missing group become sealable within the same call.
/// </summary>
/// <param name="SourceSystemId">Only orders imported from this external system; null for every source.</param>
/// <param name="FromDate">Only orders starting on or after this date; null for no lower bound.</param>
/// <param name="UntilDate">Only orders starting on or before this date; null for no upper bound.</param>
/// <param name="CustomerName">Case-insensitive fragment the customer's name or company must contain; null for every customer.</param>
/// <param name="GroupId">Only orders already linked to this group; null ignores group membership.</param>
/// <param name="MaxCount">Upper bound on the number of orders processed; null processes every match.</param>
/// <param name="AutoAssignGroups">When true, the group assignment runs before the sealing pass.</param>
/// <param name="ValidFrom">Start date of the group links the assignment creates; null defaults to today.</param>
/// <param name="Apply">False for a dry-run preview, true to perform the sealing.</param>
/// <param name="UserName">Name of the acting user, stored on the created group links.</param>
public record SealOpenOrdersCommand(
    string? SourceSystemId,
    DateOnly? FromDate,
    DateOnly? UntilDate,
    string? CustomerName,
    Guid? GroupId,
    int? MaxCount,
    bool AutoAssignGroups,
    DateTime? ValidFrom,
    bool Apply,
    string UserName) : IRequest<SealOpenOrdersResult>;
