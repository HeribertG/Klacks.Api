// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as a PANTHEON (Datalab) "Uvoz temeljnice iz Excela" booking-journal import
/// for Croatia/Slovenia bookkeeping (Fibu/Order family, not payroll): a single-sheet .xlsx workbook
/// with one header row followed by one booking line per WorkExportEntry plus one additional
/// booking line per ExpensesExportEntry. Column order (22 columns, strictly positional): Stevilka
/// temeljnice; Datum obdobja za temeljnico; Konto; Subjekt; Debet; Kredit; Dokument; Vezni dokument;
/// Datum dokumenta; Datum zapadlosti; Valuta; Tecaj; Oddelek; Stroskovni nosilec; Opomba; Protikonto;
/// Datum DDV; Tuj dokument; Status; Naziv statusa; Vrednost Debet; Vrednost Kredit.
/// </summary>
/// <remarks>
/// The 22 column names and their exact order are verified against the live PANTHEON user manual
/// (Slovenian localization, checked 2026-07-09):
/// https://usersite.datalab.eu/pantheonusermanual/tabid/316/language/sl-si/topic/uvoz-temeljnice-iz-excela/htmlid/1002433/default.aspx,
/// which states "Excel datoteka mora vsebovati sledece stolpce v sledecem vrstnem redu" (the Excel
/// file must contain the following columns in the following order) and lists exactly these 22 names.
/// The Croatian installation of PANTHEON almost certainly renders the same columns under Croatian
/// labels (e.g. Duguje/Potrazuje instead of Debet/Kredit) rather than the Slovenian ones emitted
/// here; since the import is strictly positional, not header-text-based, this is not expected to
/// affect a successful import but the Croatian header strings themselves are unverified.
///
/// The manual documents the column names but not what each column individually represents beyond
/// its name, so the mapping of Klacks data onto these columns below is inferred by analogy to this
/// project's other order/Fibu formatters (BmdExportFormatter, MoveinIlExportFormatter), not
/// independently confirmed against a real PANTHEON-imported sample:
/// - Each row is treated as one compound double-entry line: Konto is the debited account, Protikonto
///   the offsetting (credited) account, the amount is written into Debet, and Kredit is left empty.
///   Whether PANTHEON instead expects two half-rows (one per side) per booking could not be verified.
/// - Klacks has no chart-of-accounts for Croatia/Slovenia, so Konto uses the employee's personnel
///   number as a placeholder debit account, and Protikonto uses a placeholder wages-payable /
///   taxable-expense / non-taxable-expense account, mirroring the placeholder-account approach
///   already used by BmdExportFormatter and MoveinIlExportFormatter.
/// - The amount posted is WorkTime + Surcharges (hours), the same unit BmdExportFormatter and
///   MoveinIlExportFormatter post, not a currency amount, since Klacks has no rate/pricing model for
///   orders. Values are rounded to 2 decimals: the manual documents this as a hard import
///   requirement for Debet/Kredit ("max 2 decimal places, otherwise error").
/// - Valuta/Tecaj (currency/exchange rate) are set to the export's currency code and an identity
///   rate of 1, since Klacks has no foreign-currency posting model. Vrednost Debet/Vrednost Kredit
///   (columns 21-22, the foreign-currency equivalents) are intentionally left blank: the manual
///   states PANTHEON calculates them automatically from the exchange rate when not supplied.
/// - Datum zapadlosti (due date), Datum DDV (VAT date), Tuj dokument (foreign document) and
///   Status/Naziv statusa are left blank: Klacks' export model has no due-date, VAT-date or
///   PANTHEON workflow-status concept, and guessing a status code risks rejection by the importer.
/// </remarks>
using System.Globalization;
using ClosedXML.Excel;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class TemeljnicaHrSiExportFormatter : IExportFormatter
{
    private const string SheetName = "Temeljnica";
    private const string DateCellFormat = "yyyy-mm-dd";
    private const int HeaderRowNumber = 1;
    private const string PlaceholderDebitAccountWages = "3600";
    private const string PlaceholderCreditAccountTaxableExpense = "7600";
    private const string PlaceholderCreditAccountNonTaxableExpense = "7800";
    private const decimal IdentityExchangeRate = 1m;
    private const int AmountDecimalPlaces = 2;

    private static readonly string[] ColumnHeaders =
    {
        "Številka temeljnice",
        "Datum obdobja za temeljnico",
        "Konto",
        "Subjekt",
        "Debet",
        "Kredit",
        "Dokument",
        "Vezni dokument",
        "Datum dokumenta",
        "Datum zapadlosti",
        "Valuta",
        "Tečaj",
        "Oddelek",
        "Stroškovni nosilec",
        "Opomba",
        "Protikonto",
        "Datum DDV",
        "Tuj dokument",
        "Status",
        "Naziv statusa",
        "Vrednost Debet",
        "Vrednost Kredit",
    };

    public string FormatKey => ExportConstants.FormatTemeljnicaHrSi;

    public string ContentType => ExportConstants.ContentTypeXlsx;

    public string FileExtension => ".xlsx";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var currencyCode = string.IsNullOrWhiteSpace(options.CurrencyCode) ? ExportConstants.DefaultCurrencyCode : options.CurrencyCode;

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName);

        for (var i = 0; i < ColumnHeaders.Length; i++)
        {
            sheet.Cell(HeaderRowNumber, i + 1).Value = ColumnHeaders[i];
        }

        var row = HeaderRowNumber + 1;
        var journalNumber = 1;

        foreach (var order in data.Orders)
        {
            var subject = order.CustomerName ?? order.OrderName;
            var linkedDocument = order.SourceSystemId ?? string.Empty;

            foreach (var work in order.WorkEntries)
            {
                var workAmount = work.WorkTime + work.Surcharges
                    + work.Changes.Sum(c => c.ChangeTime + c.Surcharges);

                WriteRow(
                    sheet,
                    row,
                    journalNumber,
                    work.WorkDate,
                    work.EmployeeIdNumber.ToString(CultureInfo.InvariantCulture),
                    subject,
                    workAmount,
                    order.ExternalOrderReference ?? journalNumber.ToString(CultureInfo.InvariantCulture),
                    linkedDocument,
                    work.WorkDate,
                    currencyCode,
                    order.OrderAbbreviation,
                    order.OrderName,
                    $"{work.EmployeeName} - {order.OrderName}",
                    PlaceholderDebitAccountWages);
                row++;
                journalNumber++;

                foreach (var expense in work.Expenses)
                {
                    var counterAccount = expense.Taxable ? PlaceholderCreditAccountTaxableExpense : PlaceholderCreditAccountNonTaxableExpense;

                    WriteRow(
                        sheet,
                        row,
                        journalNumber,
                        work.WorkDate,
                        work.EmployeeIdNumber.ToString(CultureInfo.InvariantCulture),
                        subject,
                        expense.Amount,
                        order.ExternalOrderReference ?? journalNumber.ToString(CultureInfo.InvariantCulture),
                        linkedDocument,
                        work.WorkDate,
                        currencyCode,
                        order.OrderAbbreviation,
                        order.OrderName,
                        $"{expense.Description} - {work.EmployeeName}",
                        counterAccount);
                    row++;
                    journalNumber++;
                }
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteRow(
        IXLWorksheet sheet,
        int row,
        int journalNumber,
        DateOnly periodDate,
        string account,
        string subject,
        decimal debitAmount,
        string document,
        string linkedDocument,
        DateOnly documentDate,
        string currencyCode,
        string department,
        string costCarrier,
        string note,
        string counterAccount)
    {
        sheet.Cell(row, 1).Value = journalNumber;

        sheet.Cell(row, 2).Value = periodDate.ToDateTime(TimeOnly.MinValue);
        sheet.Cell(row, 2).Style.DateFormat.Format = DateCellFormat;

        sheet.Cell(row, 3).Value = account;
        sheet.Cell(row, 4).Value = subject;
        sheet.Cell(row, 5).Value = Math.Round(debitAmount, AmountDecimalPlaces, MidpointRounding.AwayFromZero);
        sheet.Cell(row, 7).Value = document;
        sheet.Cell(row, 8).Value = linkedDocument;

        sheet.Cell(row, 9).Value = documentDate.ToDateTime(TimeOnly.MinValue);
        sheet.Cell(row, 9).Style.DateFormat.Format = DateCellFormat;

        sheet.Cell(row, 11).Value = currencyCode;
        sheet.Cell(row, 12).Value = IdentityExchangeRate;
        sheet.Cell(row, 13).Value = department;
        sheet.Cell(row, 14).Value = costCarrier;
        sheet.Cell(row, 15).Value = note;
        sheet.Cell(row, 16).Value = counterAccount;
    }
}
