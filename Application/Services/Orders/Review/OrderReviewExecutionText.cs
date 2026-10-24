// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The plain-language description of what execute_order_review does with the orders that are ready, shared by
/// preview_order_review and the confirmation preview of execute_order_review so the user confirms exactly what was
/// shown. First the section "Changes to orders" (a new start changes the order itself, with what the start covers at
/// the edges of the period, the group, the required qualifications and the default expenses written to the order),
/// then per order the sealing and the cut into its parts, each part carrying the expenses. SelectReady picks the ready
/// orders of the targets, at most OrderReviewDefaults.MaxOrdersPerExecution per confirmation. Pure.
/// </summary>

using System.Globalization;
using System.Text;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant.OrderReview;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewExecutionText
{
    private const string ChangesHeader = "Changes to orders:";
    private const string StartChangeFormat = "- '{0}': start {1} -> {2} ({3}); this changes the order itself.";
    private const string GroupFormat = "- '{0}': group '{1}'.";
    private const string QualificationsFormat = "- '{0}': requires {1}.";
    private const string ExpensesFormat = "- '{0}': default expenses {1}.";
    private const string NoChanges = "- none; the orders are sealed as they are.";
    private const string ExecutionHeader = "Then, per order in one transaction:";
    private const string SealOnlyFormat = "- '{0}' ({1}): sealed into its plannable shift, not split.";
    private const string SealAndCutFormat = "- '{0}' ({1}): sealed and cut into {2}; every part carries {3}.";
    private const string NoExpenses = "no default expenses";
    private const string IrreversibleNote = "Sealing can never be undone.";
    private const string MoreReadyFormat = "{0} more ready order(s) follow in the next confirmation.";
    private const string TaxableMarker = " (taxable)";
    private const string ListSeparator = ", ";

    public static (List<OrderReviewDraftItem> Ready, int MoreReady, List<OrderReviewDraftItem> NotReady) SelectReady(
        IEnumerable<OrderReviewDraftItem> targets)
    {
        var pending = targets.Where(OrderReviewReadiness.IsPending).ToList();
        var ready = pending.Where(OrderReviewReadiness.IsReady).ToList();
        var notReady = pending.Where(item => !OrderReviewReadiness.IsReady(item)).ToList();
        var batch = ready.Take(OrderReviewDefaults.MaxOrdersPerExecution).ToList();
        return (batch, ready.Count - batch.Count, notReady);
    }

    public static string Describe(IReadOnlyList<OrderReviewDraftItem> ready, int moreReady)
    {
        var text = new StringBuilder();
        text.AppendLine(ChangesHeader);
        var changes = ready.SelectMany(OrderChanges).ToList();
        if (changes.Count == 0)
        {
            text.AppendLine(NoChanges);
        }

        changes.ForEach(change => text.AppendLine(change));
        text.AppendLine(ExecutionHeader);
        foreach (var item in ready)
        {
            text.AppendLine(item.ChosenParts is { Count: > 0 } parts
                ? string.Format(CultureInfo.InvariantCulture, SealAndCutFormat, item.OrderName,
                    OrderReviewPresenter.Period(item), OrderReviewPresenter.Parts(parts), Expenses(item))
                : string.Format(CultureInfo.InvariantCulture, SealOnlyFormat, item.OrderName, OrderReviewPresenter.Period(item)));
        }

        text.Append(IrreversibleNote);
        if (moreReady > 0)
        {
            text.Append(' ').Append(string.Format(CultureInfo.InvariantCulture, MoreReadyFormat, moreReady));
        }

        return text.ToString();
    }

    public static string Expenses(OrderReviewDraftItem item) =>
        item.Expenses.Count == 0
            ? NoExpenses
            : string.Join(ListSeparator, item.Expenses.Select(expense =>
                expense.Description + " " + expense.Amount.ToString(CultureInfo.InvariantCulture)
                + (expense.Taxable ? TaxableMarker : string.Empty)));

    private static IEnumerable<string> OrderChanges(OrderReviewDraftItem item)
    {
        if (OrderReviewReadiness.ChangesStart(item))
        {
            yield return string.Format(CultureInfo.InvariantCulture, StartChangeFormat, item.OrderName,
                OrderReviewPresenter.Time(item.CurrentStart), OrderReviewPresenter.Time(item.ChosenStart!.Value),
                OrderReviewPresenter.Coverage(item));
        }

        if (!item.HasGroup && item.ChosenGroupName != null)
        {
            yield return string.Format(CultureInfo.InvariantCulture, GroupFormat, item.OrderName, item.ChosenGroupName);
        }

        if (item.ChosenQualificationNames.Count > 0)
        {
            yield return string.Format(CultureInfo.InvariantCulture, QualificationsFormat, item.OrderName,
                string.Join(ListSeparator, item.ChosenQualificationNames));
        }

        if (item.Expenses.Count > 0)
        {
            yield return string.Format(CultureInfo.InvariantCulture, ExpensesFormat, item.OrderName, Expenses(item));
        }
    }
}
