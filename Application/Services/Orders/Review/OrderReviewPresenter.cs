// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Renders the order review for the assistant: one entry per review key - a bundle with its shared pattern and a few of
/// its orders, or a single order with its outlier reason - stating the start (current, proposed, chosen) with what it
/// covers at the edges of the period, the split with the rule verdict, the group and where its proposal comes from,
/// the qualifications, the expenses and what is still open. Times are written invariantly (HH:mm). Pure.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewPresenter
{
    public const int MaxListedOrdersPerKey = 10;

    private const string PartSeparator = ", ";
    private const string RangeFormat = "{0}-{1}";
    private const string OpenEnd = "open end";
    private const string NoSplit = "no split";
    private const string FirstDayUncoveredFormat = "first day uncovered from 00:00 to {0}";
    private const string LastDayRunsOverFormat = "the last service runs to {0} on the day after the last day";
    private const string LastDayEndsAtMidnight = "the last day ends at midnight";
    private const string WholeFirstDayCovered = "the first day is covered from midnight";
    private const string VerdictAllowedFormat = "allowed by the rules of all {0} group members";
    private const string VerdictSomeFormat = "allowed by the rules of only {0} of {1} group members - offer it with this hint";
    private const string VerdictNone = "not allowed by the rules of any group member";
    private const string VerdictNoMembers = "the group has no members to check the rules against";

    public static List<object> DescribeKeys(IEnumerable<OrderReviewDraftItem> items, int maxKeys)
    {
        return items
            .GroupBy(item => item.ReviewKey, StringComparer.Ordinal)
            .OrderBy(group => group.Key.StartsWith(OrderReviewDefaults.SingleKeyPrefix, StringComparison.Ordinal))
            .ThenBy(group => KeyNumber(group.Key))
            .Take(maxKeys)
            .Select(group => DescribeKey(group.Key, group.ToList()))
            .ToList();
    }

    public static object DescribeKey(string key, IReadOnlyList<OrderReviewDraftItem> items)
    {
        var pattern = items[0];
        return new
        {
            Key = key,
            OrderCount = items.Count,
            pattern.OutlierReason,
            Orders = items.Take(MaxListedOrdersPerKey).Select(item => new
            {
                item.OrderId,
                item.OrderName,
                item.CustomerName,
                Period = Period(item),
                Status = item.ExecutionStatus.ToString(),
                item.FailureReason,
            }).ToList(),
            MoreOrders = Math.Max(0, items.Count - MaxListedOrdersPerKey),
            Times = string.Format(CultureInfo.InvariantCulture, RangeFormat, Time(pattern.CurrentStart), Time(pattern.CurrentEnd)),
            pattern.IsFullDay,
            Start = DescribeStart(pattern),
            Split = DescribeSplit(pattern),
            Group = DescribeGroup(pattern),
            Qualifications = DescribeQualifications(pattern),
            Expenses = DescribeExpenses(pattern),
            pattern.BlockingFields,
            OpenAspects = items.Where(OrderReviewReadiness.IsPending)
                .SelectMany(OrderReviewReadiness.OpenAspects).Distinct().ToList(),
            PendingCount = items.Count(OrderReviewReadiness.IsPending),
        };
    }

    public static string Parts(IEnumerable<OrderReviewPart> parts)
    {
        var list = parts.ToList();
        return list.Count == 0
            ? NoSplit
            : string.Join(PartSeparator, list.Select(part =>
                string.Format(CultureInfo.InvariantCulture, RangeFormat, Time(part.Start), Time(part.End))
                + (string.IsNullOrWhiteSpace(part.Name) ? string.Empty : " " + part.Name)));
    }

    public static string Coverage(OrderReviewDraftItem item)
    {
        var start = item.ChosenStart ?? item.ProposedStart ?? item.CurrentStart;
        var parts = item.ChosenParts ?? item.ProposedParts;
        var coverage = FullDayOrderRule.Coverage(start, parts);
        var first = coverage.FirstDayUncoveredUntil is { } until
            ? string.Format(CultureInfo.InvariantCulture, FirstDayUncoveredFormat, Time(until))
            : WholeFirstDayCovered;
        var last = coverage.LastDayEndsNextDayAt is { } end
            ? string.Format(CultureInfo.InvariantCulture, LastDayRunsOverFormat, Time(end))
            : LastDayEndsAtMidnight;
        return first + "; " + last;
    }

    public static string Verdict(OrderReviewDraftItem item) => item.SplitVerdict switch
    {
        OrderSplitVerdictKind.Allowed => string.Format(CultureInfo.InvariantCulture, VerdictAllowedFormat, item.SplitMemberCount),
        OrderSplitVerdictKind.AllowedForSome => string.Format(
            CultureInfo.InvariantCulture, VerdictSomeFormat, item.SplitAllowedMembers, item.SplitMemberCount),
        OrderSplitVerdictKind.NotAllowed => VerdictNone,
        OrderSplitVerdictKind.NoMembers => VerdictNoMembers,
        _ => string.Empty,
    };

    public static string Time(TimeOnly time) => time.ToString(DateTimeFormats.HourMinute, CultureInfo.InvariantCulture);

    public static string Period(OrderReviewDraftItem item) =>
        item.FromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture) + " - "
        + (item.UntilDate?.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture) ?? OpenEnd);

    private static object DescribeStart(OrderReviewDraftItem item) => new
    {
        Current = Time(item.CurrentStart),
        Proposed = item.ProposedStart.HasValue ? Time(item.ProposedStart.Value) : null,
        Chosen = item.ChosenStart.HasValue ? Time(item.ChosenStart.Value) : null,
        ChangesTheOrder = OrderReviewReadiness.ChangesStart(item) || (item.ChosenStart == null && item.ProposedStart.HasValue),
        Coverage = item.IsFullDay ? Coverage(item) : null,
    };

    private static object DescribeSplit(OrderReviewDraftItem item) => new
    {
        Proposed = item.ProposedParts.Count > 0 ? Parts(item.ProposedParts) : null,
        Chosen = item.ChosenParts != null ? Parts(item.ChosenParts) : null,
        Rules = Verdict(item),
    };

    private static object DescribeGroup(OrderReviewDraftItem item) => new
    {
        Current = item.CurrentGroupName,
        Proposed = item.ProposedGroupName,
        ProposedBecause = item.HasGroup ? null : item.GroupSource,
        Chosen = item.ChosenGroupName,
    };

    private static object DescribeQualifications(OrderReviewDraftItem item) => new
    {
        Proposed = item.ProposedQualificationNames,
        Chosen = item.ChosenQualificationIds != null ? item.ChosenQualificationNames : null,
    };

    private static object DescribeExpenses(OrderReviewDraftItem item) => new
    {
        Status = item.ExpenseStatus.ToString(),
        Items = item.Expenses.Select(expense => new { expense.Description, expense.Amount, expense.Taxable }).ToList(),
    };

    private static int KeyNumber(string key) =>
        int.TryParse(key.AsSpan(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) ? number : int.MaxValue;
}
