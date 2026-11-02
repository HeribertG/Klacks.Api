// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports order data as a BMD NTCS FIBU booking import (Austria): semicolon-separated CSV whose first row
/// carries the standard BMD column headings, so BMD maps the columns by name without a manual field assignment.
/// Each work entry and each expense becomes one booking with Buchungssymbol ER, Buchungscode 2 (credit) and a
/// negated amount, the pattern the FIBU04 interface description shows for ER bookings.
/// After the standard columns follow Klacks reference columns (auftragsname, auftragskuerzel, stunden, kundennr,
/// kundenname, extbelegnr, quellsystem, extkundenref). The order name goes to auftragsname, not to the BMD field
/// kost (numeric Kostenstelle), which stays out of the file. Text is cut to the BMD limit of 40 characters
/// (employee name first, then the order name or expense description).
/// Only hour-bearing changes (corrections, travel) add to the amount; replacements and briefing/debriefing do not.
/// The amounts are hours (work time plus surcharge time) and expense amounts, not monetary values; the stunden
/// column carries the plain work time. All numeric values, including stunden, use the de-AT decimal comma.
/// Konto (employee number) and Gkonto (fixed counter accounts per booking kind) are placeholders that have to be
/// mapped in BMD. The layout follows the FIBU04 interface description cited below and has not been confirmed
/// against an official BMD import specification or a BMD test import.
/// </summary>
/// <remarks>
/// Source: EDV Hausleitner, "BMD NTCS Schnittstelle FIBU04", version 1.0-2 of 14.05.2020, chapter 3 (minimum
/// column headings) and 3.2.2 (ER booking: Buchungscode fixed 2, amount multiplied by -1). Klacks has no tax
/// data and no payment terms, so Steuercode, Prozent, Steuer, ZZiel, Skontopz and Skontotage stay empty.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class BmdExportFormatter : IExportFormatter
{
    private const string Separator = ";";
    private const int Windows1252CodePage = 1252;
    private const string AmountFormat = "F2";
    private const string AmountCultureName = "de-AT";
    private const string BmdSatzartBuchung = "0";
    private const string BmdBuchsymbolIncomingInvoice = "ER";
    private const string BmdBuchcodeCredit = "2";
    private const string WorkCounterAccount = "3600";
    private const string TaxableExpenseCounterAccount = "7600";
    private const string NonTaxableExpenseCounterAccount = "7800";
    private const int BmdTextMaxLength = 40;

    private static readonly string[] HeaderColumns =
    [
        "satzart", "Konto", "Gkonto", "Belegnr", "Buchdatum", "Belegdatum", "Buchsymbol", "Buchcode",
        "Steuercode", "Betrag", "Prozent", "Steuer", "Text", "ZZiel", "Skontopz", "Skontotage",
        "auftragsname", "auftragskuerzel", "stunden", "kundennr", "kundenname",
        "extbelegnr", "quellsystem", "extkundenref",
    ];

    public string FormatKey => ExportConstants.FormatBmd;

    public string ContentType => ExportConstants.ContentTypeCsv;

    public string FileExtension => ".csv";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();

        AppendLine(sb, string.Join(Separator, HeaderColumns));

        var bookingNumber = 1;

        foreach (var order in data.Orders)
        {
            var orderColumns = string.Join(Separator,
                EscapeBmd(order.OrderName),
                EscapeBmd(order.OrderAbbreviation));
            var customerColumns = string.Join(Separator,
                order.CustomerNumber?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                EscapeBmd(order.CustomerName ?? string.Empty),
                EscapeBmd(order.ExternalOrderReference ?? string.Empty),
                EscapeBmd(order.SourceSystemId ?? string.Empty),
                EscapeBmd(order.CustomerExternalReference ?? string.Empty));

            foreach (var work in order.WorkEntries)
            {
                var totalAmount = OrderWorkHours.BillableHours(work);

                AppendBooking(
                    sb,
                    work,
                    WorkCounterAccount,
                    bookingNumber,
                    totalAmount,
                    EscapeBmdText($"{work.EmployeeName} - {order.OrderName}"),
                    FormatAmount(work.WorkTime),
                    orderColumns,
                    customerColumns);

                bookingNumber++;

                foreach (var expense in work.Expenses)
                {
                    AppendBooking(
                        sb,
                        work,
                        expense.Taxable ? TaxableExpenseCounterAccount : NonTaxableExpenseCounterAccount,
                        bookingNumber,
                        expense.Amount,
                        EscapeBmdText($"{expense.Description} - {work.EmployeeName}"),
                        string.Empty,
                        orderColumns,
                        customerColumns);

                    bookingNumber++;
                }
            }
        }

        return Encoding.GetEncoding(Windows1252CodePage).GetBytes(sb.ToString());
    }

    private static void AppendBooking(
        StringBuilder sb,
        WorkExportEntry work,
        string counterAccount,
        int bookingNumber,
        decimal amount,
        string text,
        string hours,
        string orderColumns,
        string customerColumns)
    {
        var bookingDate = work.WorkDate.ToString(DateTimeFormats.DottedDate, CultureInfo.InvariantCulture);

        AppendLine(sb, string.Join(Separator,
            BmdSatzartBuchung,
            work.EmployeeIdNumber.ToString(CultureInfo.InvariantCulture),
            counterAccount,
            bookingNumber.ToString(CultureInfo.InvariantCulture),
            bookingDate,
            bookingDate,
            BmdBuchsymbolIncomingInvoice,
            BmdBuchcodeCredit,
            string.Empty,
            FormatAmount(Negate(amount)),
            string.Empty,
            string.Empty,
            text,
            string.Empty,
            string.Empty,
            string.Empty,
            orderColumns,
            hours,
            customerColumns));
    }

    private static void AppendLine(StringBuilder sb, string line)
    {
        sb.Append(line).Append(ExportConstants.LineEnding);
    }

    private static decimal Negate(decimal value)
    {
        return value == 0m ? 0m : -value;
    }

    private static string FormatAmount(decimal value)
    {
        return value.ToString(AmountFormat, CultureInfo.GetCultureInfo(AmountCultureName));
    }

    private static string EscapeBmdText(string value)
    {
        return EscapeBmd(value, BmdTextMaxLength);
    }

    private static string EscapeBmd(string value, int? maxLength = null)
    {
        value = CsvFormulaGuard.Neutralize(value);

        if (maxLength is int max && value.Length > max)
        {
            value = value[..max];
        }

        if (value.Contains('"') || value.Contains(';') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
