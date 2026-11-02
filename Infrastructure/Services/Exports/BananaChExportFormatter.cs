// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports order data as a Banana Accounting (Banana Buchhaltung, Banana.ch SA) double-entry
/// transaction import in Banana's own "Text file with column headers" format (Actions &gt; Import into
/// accounting &gt; Transactions). Tab-separated, one header row of English NameXml column
/// names followed by one row per booking: Date; Doc; DocOriginal; Description; AccountDebit;
/// AccountCredit; Amount (German UI labels: Datum, Beleg, Originalbeleg, Beschreibung, KtSoll, KtHaben,
/// Betrag). One row per WorkExportEntry and one further row per ExpensesExportEntry.
/// </summary>
/// <remarks>
/// Spec source: Banana developer guide "Importing Data into Banana Accounting", sections "Text file
/// with column headers" and "Import Double-entry transactions in CSV format"
/// (https://www.banana.ch/doc/en/book/export/html/3713, mirrored at
/// https://www.banana.ch/doc/pt-br/book/export/html/3713; overview https://www.banana.ch/doc7/en/node/3328).
/// Confirmed directly from that spec: fields and column names are separated by the tab character;
/// each line after a "\n" is a new record; the first line holds the column names, which are
/// case-sensitive and must equal the English NameXml of the Transactions table columns (Date,
/// Description, AccountDebit, AccountCredit, Amount, plus any further column such as DocOriginal);
/// dates are yyyy-mm-dd; the decimal separator is "." with no thousands separator; the encoding should
/// preferably be UTF-8 (the import dialog offers a "Unicode (UTF-8)" option), otherwise the local
/// encoding. The column set Date/Doc/Description/AccountDebit/AccountCredit/Amount is taken verbatim
/// from the spec's own example file; DocOriginal ("the document number, for example the invoice
/// number") is one of the spec's suggested additional columns and carries the ERP order reference.
/// Not stated by the spec and therefore chosen conservatively here: the file is UTF-8 without a BOM
/// (so the first header cell reads exactly "Date"), records end with "\n" as documented, and no text
/// qualifier/quoting is used because the spec defines none - tabs, CR and LF in free text are replaced
/// by a space and double quotes by a single quote so a value can never split a record or be mistaken
/// for a quoted field; CsvFormulaGuard is applied as in the other text formatters because such files
/// are often opened in a spreadsheet first. No field-length limit is documented, so nothing is
/// truncated. The spec does not distinguish Banana Accounting Plus from Banana 9 for this format; it
/// has not been checked against a real import in either version.
/// Klacks holds a Leistungsnachweis (hours), not monetary values, and has no chart of accounts. Like
/// Sie4bSeExportFormatter, ZohoBooksAeExportFormatter, MoveinIlExportFormatter and BmdExportFormatter,
/// Amount therefore carries the time quantity (WorkTime + Surcharges + changes) for work rows and the
/// expense amount for expense rows, and the accounts are placeholders modelled on the Swiss
/// KMU-Kontenrahmen that the customer must map to their own Banana chart before importing (the spec
/// does not say how Banana treats an account missing from the chart): PlaceholderWagesAccount "5000"
/// (Lohnaufwand, debit for worked time), PlaceholderPayablesAccount "2210" (Sonstige kurzfristige
/// Verbindlichkeiten, credit for every row), PlaceholderTaxableExpenseAccount "5800" (Uebriger
/// Personalaufwand, debit for taxable allowances) and PlaceholderNonTaxableExpenseAccount "5820"
/// (Spesenentschaedigungen effektiv, debit for non-taxable expense reimbursements). VatCode and
/// AmountCurrency are not written (no VAT model, single accounting currency). Doc is a running
/// booking number across all orders, starting at 1.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class BananaChExportFormatter : IExportFormatter
{
    private const string Separator = "\t";
    private const string LineEnding = "\n";
    private const string AmountFormat = "F2";
    private const int AmountDecimalPlaces = 2;
    private const string TextReplacement = " ";
    private const string DoubleQuote = "\"";
    private const string SingleQuote = "'";
    private const string PlaceholderWagesAccount = "5000";
    private const string PlaceholderPayablesAccount = "2210";
    private const string PlaceholderTaxableExpenseAccount = "5800";
    private const string PlaceholderNonTaxableExpenseAccount = "5820";

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly string[] ColumnHeaders =
    {
        "Date",
        "Doc",
        "DocOriginal",
        "Description",
        "AccountDebit",
        "AccountCredit",
        "Amount",
    };

    public string FormatKey => ExportConstants.FormatBananaCh;

    public string ContentType => ExportConstants.ContentTypeOctetStream;

    public string FileExtension => ".txt";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        sb.Append(string.Join(Separator, ColumnHeaders));
        sb.Append(LineEnding);

        var documentNumber = 1;

        foreach (var order in data.Orders)
        {
            var reference = order.ExternalOrderReference;

            foreach (var work in order.WorkEntries)
            {
                var totalAmount = work.WorkTime + work.Surcharges
                    + work.Changes.Sum(c => c.ChangeTime + c.Surcharges);

                AppendRow(
                    sb,
                    work.WorkDate,
                    documentNumber,
                    reference,
                    $"{work.EmployeeName} - {order.OrderName}",
                    PlaceholderWagesAccount,
                    PlaceholderPayablesAccount,
                    totalAmount);
                documentNumber++;

                foreach (var expense in work.Expenses)
                {
                    var debitAccount = expense.Taxable
                        ? PlaceholderTaxableExpenseAccount
                        : PlaceholderNonTaxableExpenseAccount;

                    AppendRow(
                        sb,
                        work.WorkDate,
                        documentNumber,
                        reference,
                        $"{expense.Description} - {work.EmployeeName}",
                        debitAccount,
                        PlaceholderPayablesAccount,
                        expense.Amount);
                    documentNumber++;
                }
            }
        }

        return Utf8WithoutBom.GetBytes(sb.ToString());
    }

    private static void AppendRow(
        StringBuilder sb,
        DateOnly bookingDate,
        int documentNumber,
        string? reference,
        string description,
        string debitAccount,
        string creditAccount,
        decimal amount)
    {
        sb.Append(string.Join(
            Separator,
            bookingDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            documentNumber.ToString(CultureInfo.InvariantCulture),
            Sanitize(reference ?? string.Empty),
            Sanitize(description),
            debitAccount,
            creditAccount,
            FormatAmount(amount)));
        sb.Append(LineEnding);
    }

    private static string FormatAmount(decimal amount)
    {
        var rounded = Math.Round(amount, AmountDecimalPlaces, MidpointRounding.AwayFromZero);
        var normalized = rounded == 0m ? 0m : rounded;
        return normalized.ToString(AmountFormat, CultureInfo.InvariantCulture);
    }

    private static string Sanitize(string value)
    {
        var sanitized = value
            .Replace("\r\n", TextReplacement)
            .Replace(Separator, TextReplacement)
            .Replace("\r", TextReplacement)
            .Replace("\n", TextReplacement)
            .Replace(DoubleQuote, SingleQuote);

        return CsvFormulaGuard.Neutralize(sanitized);
    }
}
