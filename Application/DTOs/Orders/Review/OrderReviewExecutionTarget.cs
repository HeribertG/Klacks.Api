// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// One order a confirmed execute_order_review changes, seals and cuts: its review decisions and the order as it is now.
/// </summary>
/// <param name="Item">The review decisions for the order</param>
/// <param name="Order">The order (status OriginalOrder) as loaded for the preview, with its groups, expenses and qualifications</param>
public sealed record OrderReviewExecutionTarget(OrderReviewDraftItem Item, Shift Order);
