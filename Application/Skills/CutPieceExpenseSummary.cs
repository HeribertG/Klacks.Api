// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Skills;

/// <summary>
/// Describes the default expenses a cut piece carries, for the result of the cut skills, so the user is told that every
/// piece is booked with them. Non-taxable rows are expenses (reimbursed outlays), taxable rows are allowances.
/// </summary>
public static class CutPieceExpenseSummary
{
    private const string NoExpenses = "no default expenses";
    private const string NonTaxableLabel = "non-taxable expense";
    private const string TaxableLabel = "taxable allowance";
    private const string Separator = ", ";

    /// <summary>One line of text naming every row with amount and kind, or a note that the piece carries none.</summary>
    /// <param name="expenses">Default expense rows of one piece</param>
    public static string Describe(IReadOnlyCollection<ShiftExpenses> expenses)
        => expenses.Count == 0
            ? NoExpenses
            : string.Join(Separator, expenses.Select(expense =>
                $"{expense.Description} {expense.Amount.ToString(CultureInfo.InvariantCulture)} " +
                $"({(expense.Taxable ? TaxableLabel : NonTaxableLabel)})"));

    /// <summary>The rows as plain result data (description, amount, taxable).</summary>
    /// <param name="expenses">Default expense rows of one piece</param>
    public static IReadOnlyList<object> ToData(IEnumerable<ShiftExpenses> expenses)
        => expenses
            .Select(expense => (object)new { expense.Description, expense.Amount, expense.Taxable })
            .ToList();
}
