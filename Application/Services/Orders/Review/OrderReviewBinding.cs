// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one place that turns the orders of an execute_order_review into the facts its confirmation is bound to (kind
/// OrderReview): per order its id and last update (an order edited after the preview voids the confirmation), the start
/// change, the parts (times and names), the group it gets, the qualifications it will require and the expenses it gets.
/// The preview provider stores the binding with the token; the skill compares it with its own fresh plan before it
/// writes anything.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewBinding
{
    private const string IdFormat = "N";
    private const string OrderPrefix = "order:";
    private const string FieldSeparator = "|";
    private const string ListSeparator = ";";
    private const string ValueSeparator = "~";
    private const string StartArrow = "->";
    private const string None = "-";

    public static ConfirmationBinding Of(IEnumerable<OrderReviewExecutionTarget> targets) =>
        ConfirmationBinding.Of(ConfirmationBindingKinds.OrderReview, targets.Select(Item));

    private static string Item(OrderReviewExecutionTarget target)
    {
        var item = target.Item;
        return OrderPrefix + string.Join(
            FieldSeparator,
            item.OrderId.ToString(IdFormat),
            target.Order.UpdateTime?.Ticks.ToString(CultureInfo.InvariantCulture) ?? None,
            OrderReviewPresenter.Time(item.CurrentStart) + StartArrow
                + OrderReviewPresenter.Time(item.ChosenStart ?? item.CurrentStart),
            string.Join(ListSeparator, (item.ChosenParts ?? []).Select(part =>
                OrderReviewPresenter.Time(part.Start) + ValueSeparator + OrderReviewPresenter.Time(part.End)
                + ValueSeparator + part.Name)),
            item.HasGroup ? None : item.ChosenGroupId?.ToString(IdFormat) ?? None,
            string.Join(ListSeparator, (item.ChosenQualificationIds ?? []).OrderBy(id => id).Select(id => id.ToString(IdFormat))),
            string.Join(ListSeparator, item.Expenses.Select(expense =>
                expense.Description + ValueSeparator + expense.Amount.ToString(CultureInfo.InvariantCulture)
                + ValueSeparator + expense.Taxable.ToString(CultureInfo.InvariantCulture))));
    }
}
