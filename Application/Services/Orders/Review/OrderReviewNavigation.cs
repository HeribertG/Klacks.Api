// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the navigation after a confirmed execute_order_review from the orders that were executed successfully. The
/// plannable shift is a clone of its sealed order (same FromDate/UntilDate; the review changes the start time, never the
/// dates, and the binding refuses an order changed since the preview), so the dates come from the executed orders without
/// another read. The period runs from the earliest start to the latest end; when one order is open-ended there is no end,
/// so "until" is left out instead of pretending one. The group is the one the order is staffed from - its own group, else
/// the group the review attached - and with several the most frequent one (ties: first executed).
/// </summary>

using System.Globalization;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewNavigation
{
    public static OrderReviewScheduleHint? For(IReadOnlyList<OrderReviewExecutionTarget> executed)
    {
        if (executed.Count == 0)
        {
            return null;
        }

        var from = Iso(executed.Min(target => target.Order.FromDate));
        var until = executed.Any(target => target.Order.UntilDate is null)
            ? null
            : Iso(executed.Max(target => target.Order.UntilDate!.Value));
        var group = MostFrequentGroup(executed.Select(target => target.Item));

        return new OrderReviewScheduleHint(PlannableRoute(from, until), from, until, group?.Id, group?.Name);
    }

    private static string PlannableRoute(string from, string? until)
    {
        var route = $"{ShiftListViewRoutes.PlannableViewRoute}&{ShiftListViewRoutes.FromQueryParam}={from}";
        return until is null ? route : $"{route}&{ShiftListViewRoutes.UntilQueryParam}={until}";
    }

    private static (Guid Id, string? Name)? MostFrequentGroup(IEnumerable<OrderReviewDraftItem> items)
    {
        var groups = items
            .Select(item => item.HasGroup
                ? (Id: item.CurrentGroupId, Name: item.CurrentGroupName)
                : (Id: item.ChosenGroupId, Name: item.ChosenGroupName))
            .Where(group => group.Id.HasValue)
            .GroupBy(group => group.Id!.Value)
            .OrderByDescending(bucket => bucket.Count())
            .FirstOrDefault();

        return groups is null
            ? null
            : (groups.Key, groups.Select(group => group.Name).FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)));
    }

    private static string Iso(DateOnly date) => date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture);
}
