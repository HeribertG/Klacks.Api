// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parses the parameters of set_order_review_decision into an OrderReviewDecision. The start is read in the user's
/// language (SkillCalendarStringParser); expenses are "description:amount" or "description:amount:taxable", several
/// separated by semicolons, the amount with a dot or a comma as decimal separator and greater than zero; noExpenses is
/// "this" (no expenses for the targeted orders) or "all" (none for any order still open). A value that cannot be read
/// is refused with the expected format, so nothing half-parsed is stored. Pure.
/// </summary>

using System.Globalization;
using Klacks.Api.Application.DTOs.Orders.Review;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant.OrderReview;
using Klacks.Api.Domain.Services.Assistant.Skills;

namespace Klacks.Api.Application.Services.Orders.Review;

public static class OrderReviewDecisionParser
{
    public const string TargetParameter = "target";
    public const string StartTimeParameter = "startTime";
    public const string SplitParameter = "split";
    public const string PartNamesParameter = "partNames";
    public const string GroupNameParameter = "groupName";
    public const string QualificationsParameter = "qualifications";
    public const string ExpensesParameter = "expenses";
    public const string NoExpensesParameter = "noExpenses";
    public const string AcceptProposalsParameter = "acceptProposals";
    public const string SkipParameter = "skip";

    public const string NoExpensesThis = "this";
    public const string NoExpensesAll = "all";

    private const char ExpenseSeparator = ';';
    private const char ExpenseFieldSeparator = ':';
    private const char CommaDecimalSeparator = ',';
    private const char DotDecimalSeparator = '.';
    private const string TaxableMarker = "taxable";
    private const int MinExpenseFields = 2;
    private const int TaxableFieldIndex = 2;

    private const string InvalidStartFormat = "startTime '{0}' is not a time. Use HH:mm, e.g. 07:00 (00:00 keeps midnight).";
    private const string InvalidExpenseFormat =
        "Expense '{0}' cannot be read. Use 'description:amount' or 'description:amount:taxable', several separated by ';', " +
        "with an amount greater than 0.";
    private const string InvalidNoExpenses = "noExpenses must be 'this' or 'all'.";

    public static (OrderReviewDecision? Decision, string? Error) Parse(Dictionary<string, object> parameters, string? language)
    {
        TimeOnly? start = null;
        var startRaw = SkillParameterReader.Read<string>(parameters, StartTimeParameter);
        if (!string.IsNullOrWhiteSpace(startRaw))
        {
            if (!SkillCalendarStringParser.TryParseTimeOnly(startRaw.Trim(), language, out var parsed))
            {
                return (null, string.Format(InvalidStartFormat, startRaw));
            }

            start = parsed;
        }

        var (expenses, expenseError) = ParseExpenses(SkillParameterReader.Read<string>(parameters, ExpensesParameter));
        if (expenseError != null)
        {
            return (null, expenseError);
        }

        var (decline, declineError) = ParseDecline(SkillParameterReader.Read<string>(parameters, NoExpensesParameter));
        if (declineError != null)
        {
            return (null, declineError);
        }

        return (new OrderReviewDecision(
            SkillParameterReader.Read<string>(parameters, TargetParameter) ?? string.Empty,
            start,
            Trimmed(parameters, SplitParameter),
            Trimmed(parameters, PartNamesParameter),
            Trimmed(parameters, GroupNameParameter),
            Trimmed(parameters, QualificationsParameter),
            expenses,
            decline,
            SkillParameterReader.Read<bool?>(parameters, AcceptProposalsParameter) ?? false,
            SkillParameterReader.Read<bool?>(parameters, SkipParameter) ?? false), null);
    }

    private static string? Trimmed(Dictionary<string, object> parameters, string name)
    {
        var value = SkillParameterReader.Read<string>(parameters, name);
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static (List<OrderReviewExpense>? Expenses, string? Error) ParseExpenses(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null);
        }

        var expenses = new List<OrderReviewExpense>();
        foreach (var entry in raw.Split(ExpenseSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var fields = entry.Split(ExpenseFieldSeparator, StringSplitOptions.TrimEntries);
            if (fields.Length < MinExpenseFields
                || string.IsNullOrWhiteSpace(fields[0])
                || !decimal.TryParse(
                    fields[1].Replace(CommaDecimalSeparator, DotDecimalSeparator),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var amount)
                || amount <= 0)
            {
                return (null, string.Format(InvalidExpenseFormat, entry));
            }

            expenses.Add(new OrderReviewExpense
            {
                Description = fields[0],
                Amount = amount,
                Taxable = fields.Length > TaxableFieldIndex
                    && string.Equals(fields[TaxableFieldIndex], TaxableMarker, StringComparison.OrdinalIgnoreCase),
            });
        }

        return expenses.Count == 0 ? (null, string.Format(InvalidExpenseFormat, raw)) : (expenses, null);
    }

    private static (OrderReviewExpenseStatus? Decline, string? Error) ParseDecline(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (null, null);
        }

        return raw.Trim().ToLowerInvariant() switch
        {
            NoExpensesThis => (OrderReviewExpenseStatus.DeclinedOnce, null),
            NoExpensesAll => (OrderReviewExpenseStatus.DeclinedFinal, null),
            _ => (null, InvalidNoExpenses),
        };
    }
}
