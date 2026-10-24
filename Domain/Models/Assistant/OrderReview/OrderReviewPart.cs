// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// One part of the split an order is to be cut into after sealing: its times and an optional name (empty: the cut
/// derives the name from the order name and the times).
/// </summary>
namespace Klacks.Api.Domain.Models.Assistant.OrderReview;

public sealed class OrderReviewPart
{
    public TimeOnly Start { get; set; }

    public TimeOnly End { get; set; }

    public string Name { get; set; } = string.Empty;
}
