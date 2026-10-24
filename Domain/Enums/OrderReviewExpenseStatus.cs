// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Enums;

/// <summary>
/// Whether the expenses of an order were asked in the order review: not yet, given, declined for this order or
/// declined for every order still open in the review.
/// </summary>
public enum OrderReviewExpenseStatus
{
    Unasked = 0,
    Provided = 1,
    DeclinedOnce = 2,
    DeclinedFinal = 3,
}
