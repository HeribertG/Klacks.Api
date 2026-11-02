// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports order data as an Israeli MOVEIN.DAT journal-entry import (Hashavshevet/Chashavshevet,
/// cross-vendor accepted by Rivhit), using the fixed-length "detailed method" (180 characters/line,
/// no companion .PRM file). Field order: Buchungsschluessel(3); Referenz1(5); Belegdatum ddMMyy(6);
/// Referenz2(5); Faelligkeitsdatum ddMMyy(6); Waehrungscode(3); Text(22); SollKonto1(8); SollKonto2(8);
/// HabenKonto1(8); HabenKonto2(8); SollBetrag1(12); SollBetrag2(12); HabenBetrag1(12); HabenBetrag2(12);
/// SollFwBetrag1(12); SollFwBetrag2(12); HabenFwBetrag1(12); HabenFwBetrag2(12); CRLF(2).
/// </summary>
/// <remarks>
/// The MOVEIN.DAT primary spec (caspit.biz/images/Hash/MOVEIN.PDF) is a scanned, non-machine-readable
/// document; this layout is reconstructed from a verified secondary source (Sumit help-center manual)
/// and has not been checked against a real customer .DAT sample. Assumed conventions pending that
/// verification: text/account fields are left-justified and space-padded. Amount fields are Decimal(9.2):
/// written with a dot and two decimals, right-aligned and space-padded to 12 characters (e.g. "        9.75");
/// unused amount fields carry "0.00" right-aligned rather than blanks. Amounts are unsigned; a negative
/// booking swaps the debit and credit accounts and writes the absolute value, so the sign is kept.
/// Klacks has no chart-of-accounts, so Soll-Konto (debit) uses the employee's personnel number and
/// Haben-Konto (credit) is a placeholder wages-payable account, mirroring the same placeholder-account
/// approach already used by BmdExportFormatter. Foreign-currency fields are always zero (not supported).
/// The currency defaults to ILS when the export currency is empty or the global default (EUR).
/// Only hour-bearing changes (corrections, travel) add to the work amount; replacements and
/// briefing/debriefing do not.
/// Encoding: Windows-1255 (Hebrew) per industry convention, not confirmed on the spec itself.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class MoveinIlExportFormatter : IExportFormatter
{
    private const int LineLength = 180;
    private const string DateFormat = "ddMMyy";
    private const string LineEnding = "\r\n";
    private const int Windows1255CodePage = 1255;
    private const string DefaultCurrencyCode = "ILS";
    private const int AmountFieldWidth = 12;
    private const string AmountFormat = "0.00";
    private const string PlaceholderCreditAccountWages = "3600";
    private const string PlaceholderCreditAccountTaxableExpense = "7600";
    private const string PlaceholderCreditAccountNonTaxableExpense = "7800";

    public string FormatKey => ExportConstants.FormatMoveinIl;

    public string ContentType => ExportConstants.ContentTypeOctetStream;

    public string FileExtension => ".dat";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var currencyCode = ResolveCurrencyCode(options.CurrencyCode);
        var referenceNumber = 1;

        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                var totalAmount = OrderWorkHours.BillableHours(work);

                AppendLine(
                    sb,
                    referenceNumber,
                    work.WorkDate,
                    currencyCode,
                    $"{work.EmployeeName} - {order.OrderName}",
                    work.EmployeeIdNumber.ToString(CultureInfo.InvariantCulture),
                    PlaceholderCreditAccountWages,
                    totalAmount);
                referenceNumber++;

                foreach (var expense in work.Expenses)
                {
                    AppendLine(
                        sb,
                        referenceNumber,
                        work.WorkDate,
                        currencyCode,
                        $"{expense.Description} - {work.EmployeeName}",
                        work.EmployeeIdNumber.ToString(CultureInfo.InvariantCulture),
                        expense.Taxable ? PlaceholderCreditAccountTaxableExpense : PlaceholderCreditAccountNonTaxableExpense,
                        expense.Amount);
                    referenceNumber++;
                }
            }
        }

        return Encoding.GetEncoding(Windows1255CodePage).GetBytes(sb.ToString());
    }

    private static void AppendLine(
        StringBuilder sb,
        int referenceNumber,
        DateOnly bookingDate,
        string currencyCode,
        string text,
        string debitAccount,
        string creditAccount,
        decimal amount)
    {
        var date = bookingDate.ToString(DateFormat, CultureInfo.InvariantCulture);
        var reference = PadNumeric(referenceNumber.ToString(CultureInfo.InvariantCulture), 5);

        if (amount < 0m)
        {
            (debitAccount, creditAccount) = (creditAccount, debitAccount);
        }

        var amountField = FormatAmount(Math.Abs(amount));
        var zeroAmount = FormatAmount(0m);

        var line = string.Concat(
            PadText(string.Empty, 3),
            reference,
            date,
            PadText(string.Empty, 5),
            date,
            PadText(currencyCode, 3),
            PadText(text, 22),
            PadText(debitAccount, 8),
            PadText(string.Empty, 8),
            PadText(creditAccount, 8),
            PadText(string.Empty, 8),
            amountField,
            zeroAmount,
            amountField,
            zeroAmount,
            zeroAmount,
            zeroAmount,
            zeroAmount,
            zeroAmount);

        sb.Append(line);
        sb.Append(LineEnding);

        System.Diagnostics.Debug.Assert(
            line.Length == LineLength - LineEnding.Length,
            "MOVEIN.DAT detailed-method record must be exactly 180 characters including the CR/LF terminator.");
    }

    private static string ResolveCurrencyCode(string? currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode)
            || string.Equals(currencyCode, ExportConstants.DefaultCurrencyCode, StringComparison.OrdinalIgnoreCase))
        {
            return DefaultCurrencyCode;
        }

        return currencyCode;
    }

    private static string FormatAmount(decimal amount)
    {
        var rounded = Math.Round(amount, 2, MidpointRounding.AwayFromZero);
        var text = rounded.ToString(AmountFormat, CultureInfo.InvariantCulture);
        return text.Length >= AmountFieldWidth ? text[^AmountFieldWidth..] : text.PadLeft(AmountFieldWidth, ' ');
    }

    private static string PadNumeric(string value, int width)
    {
        return value.Length >= width ? value[^width..] : value.PadLeft(width, '0');
    }

    private static string PadText(string value, int width)
    {
        var sanitized = value.Replace("\r", string.Empty).Replace("\n", string.Empty);
        return sanitized.Length >= width ? sanitized[..width] : sanitized.PadRight(width, ' ');
    }
}
