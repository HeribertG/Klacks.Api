// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What is still open for an order in the review before execute_order_review may change, seal and cut it: the start of
/// a full-day order, the split, the group of an order without one, the qualifications, and the expenses (always asked,
/// never assumed). An order with blocking fields can never be executed from the review. Pure.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewReadiness
{
    public const string Start = "start";
    public const string Split = "split";
    public const string Group = "group";
    public const string Qualifications = "qualifications";
    public const string Expenses = "expenses";

    public static List<string> OpenAspects(OrderReviewDraftItem item)
    {
        var open = new List<string>();
        if (item.ChosenStart == null)
        {
            open.Add(Start);
        }

        if (item.ChosenParts == null)
        {
            open.Add(Split);
        }

        if (!item.HasGroup && item.ChosenGroupId == null)
        {
            open.Add(Group);
        }

        if (item.ChosenQualificationIds == null)
        {
            open.Add(Qualifications);
        }

        if (item.ExpenseStatus == OrderReviewExpenseStatus.Unasked)
        {
            open.Add(Expenses);
        }

        return open;
    }

    public static bool IsPending(OrderReviewDraftItem item) =>
        item.ExecutionStatus is OrderReviewExecutionStatus.Pending or OrderReviewExecutionStatus.Failed;

    public static bool IsReady(OrderReviewDraftItem item) =>
        IsPending(item) && item.BlockingFields.Count == 0 && OpenAspects(item).Count == 0;

    public static bool ChangesStart(OrderReviewDraftItem item) =>
        item.ChosenStart.HasValue && item.ChosenStart.Value != item.CurrentStart;

    public static Guid? ExecutingGroupId(OrderReviewDraftItem item) =>
        item.CurrentGroupId ?? item.ChosenGroupId ?? item.ProposedGroupId;
}
