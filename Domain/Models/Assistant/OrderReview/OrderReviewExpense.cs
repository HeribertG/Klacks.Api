// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A default expense the user named for an order in the order review; it is stored on the order before sealing, so the
/// plannable shift and every part of its cut carry it.
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant.OrderReview;

public sealed class OrderReviewExpense
{
    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public bool Taxable { get; set; }
}
