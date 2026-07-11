// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as a Zoho Books "Manual Journals" import CSV (UAE edition, Fibu/Order family):
/// for each booking it emits a balanced two-line journal (a debit line plus an offsetting credit line
/// sharing one Journal#), so every journal satisfies Zoho's sum(Debit) == sum(Credit) rule. Columns
/// (positional, with a header row): Journal Date; Journal#; Reference#; Notes; Account Name;
/// Description; Debit; Credit; Currency Code; Tax Name; Tax Percentage.
/// </summary>
/// <remarks>
/// - Debit and Credit are two separate columns (Zoho does not use a signed single column); the
///   positive amount goes into one column and the other is left empty.
/// - Zoho matches "Account Name" by name against the customer's chart of accounts. Klacks has no
///   chart of accounts, so placeholder account names are emitted (debit = wages expense, credit =
///   payable) that the customer must map to their real Zoho accounts, mirroring the placeholder-
///   account approach of BmdExportFormatter/TemeljnicaHrSiExportFormatter.
/// - The posted value is hours (WorkTime + Surcharges + WorkChange), not a currency amount, since
///   Klacks has no order pricing model — the same placeholder unit those other Fibu formatters post.
/// - Notes is mandatory system-wide in Zoho, so a non-empty note is always written; Tax Name and Tax
///   Percentage are left empty (Klacks has no VAT-posting model). Dates use ISO yyyy-MM-dd. The file
///   is UTF-8 without BOM to avoid a BOM being read into the first header cell by Zoho's CSV importer.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ZohoBooksAeExportFormatter : IExportFormatter
{
    private const string Separator = ",";
    private const string DateFormat = "yyyy-MM-dd";
    private const string AmountFormat = "F2";
    private const int AmountDecimalPlaces = 2;
    private const string PlaceholderDebitAccount = "Salaries and Employee Wages";
    private const string PlaceholderCreditAccount = "Accounts Payable";
    private const string PlaceholderExpenseDebitAccount = "Other Expenses";
    private const string NotePrefix = "Klacks hours export";

    private static readonly string[] ColumnHeaders =
    {
        "Journal Date",
        "Journal#",
        "Reference#",
        "Notes",
        "Account Name",
        "Description",
        "Debit",
        "Credit",
        "Currency Code",
        "Tax Name",
        "Tax Percentage",
    };

    public string FormatKey => ExportConstants.FormatZohoBooksAe;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var currencyCode = string.IsNullOrWhiteSpace(options.CurrencyCode)
            ? ExportConstants.DefaultCurrencyCode
            : options.CurrencyCode;

        var sb = new StringBuilder();
        sb.Append(string.Join(Separator, ColumnHeaders));
        sb.Append('\n');

        var journalNumber = 1;

        foreach (var order in data.Orders)
        {
            var reference = order.ExternalOrderReference;

            foreach (var work in order.WorkEntries)
            {
                var amount = work.WorkTime + work.Surcharges
                    + work.Changes.Sum(c => c.ChangeTime + c.Surcharges);

                WriteJournal(
                    sb,
                    journalNumber,
                    work.WorkDate,
                    reference,
                    $"{NotePrefix} - {order.OrderName}",
                    $"{work.EmployeeName} - {order.OrderName}",
                    amount,
                    PlaceholderDebitAccount,
                    PlaceholderCreditAccount,
                    currencyCode);
                journalNumber++;

                foreach (var expense in work.Expenses)
                {
                    WriteJournal(
                        sb,
                        journalNumber,
                        work.WorkDate,
                        reference,
                        $"{NotePrefix} - {order.OrderName}",
                        $"{expense.Description} - {work.EmployeeName}",
                        expense.Amount,
                        PlaceholderExpenseDebitAccount,
                        PlaceholderCreditAccount,
                        currencyCode);
                    journalNumber++;
                }
            }
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static void WriteJournal(
        StringBuilder sb,
        int journalNumber,
        DateOnly journalDate,
        string? reference,
        string notes,
        string description,
        decimal amount,
        string debitAccount,
        string creditAccount,
        string currencyCode)
    {
        var rounded = Math.Round(amount, AmountDecimalPlaces, MidpointRounding.AwayFromZero);
        var date = journalDate.ToString(DateFormat, CultureInfo.InvariantCulture);
        var journal = journalNumber.ToString(CultureInfo.InvariantCulture);
        var referenceValue = string.IsNullOrEmpty(reference) ? journal : reference;
        var amountText = rounded.ToString(AmountFormat, CultureInfo.InvariantCulture);

        WriteRow(sb, date, journal, referenceValue, notes, debitAccount, description, amountText, string.Empty, currencyCode);
        WriteRow(sb, date, journal, referenceValue, notes, creditAccount, description, string.Empty, amountText, currencyCode);
    }

    private static void WriteRow(
        StringBuilder sb,
        string date,
        string journal,
        string reference,
        string notes,
        string accountName,
        string description,
        string debit,
        string credit,
        string currencyCode)
    {
        sb.Append(string.Join(
            Separator,
            date,
            journal,
            Escape(reference),
            Escape(notes),
            Escape(accountName),
            Escape(description),
            debit,
            credit,
            currencyCode,
            string.Empty,
            string.Empty));
        sb.Append('\n');
    }

    private static string Escape(string value)
    {
        value = CsvFormulaGuard.Neutralize(value);

        if (value.Contains(Separator) || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
