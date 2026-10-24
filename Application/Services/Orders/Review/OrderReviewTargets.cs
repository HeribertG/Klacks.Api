// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the target of an order-review call to draft items: "all", review keys ("B1", "S3", case-insensitive) or
/// order ids, several separated by commas. An unknown token is answered with the keys the review has, so nothing is
/// guessed. Pure.
/// </summary>

using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewTargets
{
    public const string All = "all";

    private const char TokenSeparator = ',';
    private const string KeySeparator = ", ";
    private const string MissingTargetError =
        "target is required: a review key such as B1 or S1, an order id, several of them separated by commas, or 'all'.";
    private const string UnknownTargetFormat = "Unknown review target '{0}'. The review has the keys: {1}.";

    public static (List<OrderReviewDraftItem> Items, string? Error) Resolve(OrderReviewDraft draft, string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ([], MissingTargetError);
        }

        if (string.Equals(target.Trim(), All, StringComparison.OrdinalIgnoreCase))
        {
            return (draft.Items.ToList(), null);
        }

        var selected = new List<OrderReviewDraftItem>();
        foreach (var token in target.Split(TokenSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var matches = Guid.TryParse(token, out var orderId)
                ? draft.Items.Where(item => item.OrderId == orderId).ToList()
                : draft.Items.Where(item => string.Equals(item.ReviewKey, token, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0)
            {
                return ([], string.Format(UnknownTargetFormat, token, Keys(draft)));
            }

            selected.AddRange(matches.Where(match => !selected.Contains(match)));
        }

        return (selected, null);
    }

    private static string Keys(OrderReviewDraft draft) =>
        string.Join(KeySeparator, draft.Items.Select(item => item.ReviewKey).Distinct(StringComparer.Ordinal));
}
