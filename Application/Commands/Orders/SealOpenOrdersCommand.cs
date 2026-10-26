// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Orders;

/// <summary>
/// Seals every open order (status OriginalOrder) matching the filter, one transaction per order, so a
/// single failure never rolls back the rest of the batch. With Apply=false it only reports how many are
/// sealable and what blocks the others. With AutoAssignGroups=true the group assignment runs first, so
/// orders blocked only by a missing group become sealable within the same call. Only orders inside Scope are
/// considered at all, so orders outside it are neither sealed nor counted nor named in the result. ExpectedSelection binds an
/// apply to the orders the confirmed preview showed.
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
/// <param name="Scope">Open orders the caller may touch; required so a command can never be built without one.</param>
/// <param name="ExpectedSelection">The classified orders the user confirmed in the preview. When set, an apply seals only if the
/// matched orders still classify exactly like that, checked before anything is written, and never touches another order;
/// null applies no such check.</param>
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
    string UserName,
    OpenOrderSealingScope Scope,
    OpenOrderSealingSelection? ExpectedSelection = null) : IRequest<SealOpenOrdersResult>;
