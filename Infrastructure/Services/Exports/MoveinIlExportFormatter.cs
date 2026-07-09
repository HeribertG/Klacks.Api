// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
/// verification: text/account fields are left-justified and space-padded; amount fields are zero-padded,
/// unsigned, fixed-point with an implied 2 decimals (no decimal separator character), i.e. amount*100.
/// Klacks has no chart-of-accounts, so Soll-Konto (debit) uses the employee's personnel number and
/// Haben-Konto (credit) is a placeholder wages-payable account, mirroring the same placeholder-account
/// approach already used by BmdExportFormatter. Foreign-currency fields are always zero (not supported).
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
    private const string PlaceholderCreditAccountWages = "3600";
    private const string PlaceholderCreditAccountTaxableExpense = "7600";
    private const string PlaceholderCreditAccountNonTaxableExpense = "7800";

    public string FormatKey => ExportConstants.FormatMoveinIl;

    public string ContentType => ExportConstants.ContentTypeOctetStream;

    public string FileExtension => ".dat";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var currencyCode = string.IsNullOrWhiteSpace(options.CurrencyCode) ? DefaultCurrencyCode : options.CurrencyCode;
        var referenceNumber = 1;

        foreach (var order in data.Orders)
        {
            foreach (var work in order.WorkEntries)
            {
                var totalAmount = work.WorkTime + work.Surcharges
                    + work.Changes.Sum(c => c.ChangeTime + c.Surcharges);

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
        var amountField = FormatAmount(amount);
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

    private static string FormatAmount(decimal amount)
    {
        var cents = (long)Math.Round(Math.Abs(amount) * 100m, MidpointRounding.AwayFromZero);
        return PadNumeric(cents.ToString(CultureInfo.InvariantCulture), 12);
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
