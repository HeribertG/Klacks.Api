// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Loads what the order review works on: the caller's scope (ResolveScopeAsync) and the analysed open orders in that
/// scope (LoadItemsAsync), ready to be stored as the review draft. Reads only.
/// </summary>
public interface IOrderReviewLoader
{
    Task<OrderReviewScope> ResolveScopeAsync(SkillExecutionContext context, CancellationToken cancellationToken = default);

    Task<List<OrderReviewDraftItem>> LoadItemsAsync(
        SkillExecutionContext context,
        OrderReviewScope scope,
        string? customerName,
        CancellationToken cancellationToken = default);
}
