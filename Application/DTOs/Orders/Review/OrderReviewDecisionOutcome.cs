// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Orders.Review;

/// <summary>
/// What a set_order_review_decision call stored and what it refused (with the reason), in plain English for the
/// assistant to relay. A refused answer is not stored; the aspect stays open.
/// </summary>
public sealed class OrderReviewDecisionOutcome
{
    public List<string> Applied { get; } = [];

    public List<string> Rejected { get; } = [];

    public List<string> Hints { get; } = [];
}
