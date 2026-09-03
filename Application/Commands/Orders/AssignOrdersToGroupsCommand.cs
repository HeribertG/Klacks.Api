// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Orders;

/// <summary>
/// Derives the group of every open order (status OriginalOrder) that carries no active group link yet
/// from the address of its customer and, with Apply=true, persists those links in one transaction.
/// With Apply=false it only previews the plan. Orders that already hold a link are never touched, which
/// is what makes a second run a no-op.
/// </summary>
/// <param name="SourceSystemId">Only orders imported from this external system; null for every source.</param>
/// <param name="FromDate">Only orders starting on or after this date; null for no lower bound.</param>
/// <param name="UntilDate">Only orders starting on or before this date; null for no upper bound.</param>
/// <param name="CustomerName">Case-insensitive fragment the customer's name or company must contain; null for every customer.</param>
/// <param name="MaxCount">Upper bound on the number of orders processed; null processes every match.</param>
/// <param name="ValidFrom">Start date of the new group links; null defaults to today.</param>
/// <param name="Apply">False for a dry-run preview, true to persist the group links.</param>
/// <param name="UserName">Name of the acting user, stored on the created links.</param>
public record AssignOrdersToGroupsCommand(
    string? SourceSystemId,
    DateOnly? FromDate,
    DateOnly? UntilDate,
    string? CustomerName,
    int? MaxCount,
    DateTime? ValidFrom,
    bool Apply,
    string UserName) : IRequest<AssignOrdersToGroupsResult>;
