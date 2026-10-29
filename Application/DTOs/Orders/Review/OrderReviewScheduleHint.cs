// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// Where the chat goes after a confirmed execute_order_review: the shift list on the plannable shifts of the executed
/// orders, and what open_schedule needs to show them in the schedule next.
/// </summary>
/// <param name="Route">The shift list route with view=plannable, from and (when every order ends) until</param>
/// <param name="FromDate">Earliest start date of the executed orders, ISO yyyy-MM-dd</param>
/// <param name="UntilDate">Latest end date of the executed orders, ISO yyyy-MM-dd; null when one of them is open-ended</param>
/// <param name="GroupId">The group most of the executed orders are staffed from; null when none has a group</param>
/// <param name="GroupName">The name of that group; null when unknown</param>
public sealed record OrderReviewScheduleHint(
    string Route, string FromDate, string? UntilDate, Guid? GroupId, string? GroupName);
