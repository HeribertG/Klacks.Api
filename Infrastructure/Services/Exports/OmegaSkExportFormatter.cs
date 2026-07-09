// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Exports order data as a KROS OMEGA accounting-journal import (Slovakia), a semicolon-delimited
/// text format using three record types: R00 (format/version header, once), R01 (document header,
/// one per order) and R02 (document item, one per work entry or expense). R01 fields: record type;
/// document number; document date (dd.MM.yyyy); partner text; currency; total amount. R02 fields:
/// record type; item type (0=accounting entry, 1=reimbursement, 2=reduction); worker internal number;
/// item date (dd.MM.yyyy); text; amount.
/// </summary>
/// <remarks>
/// The source spec (docs/knowledge/export-samples/sk-kros-omega-import-fieldspec.xls) is a legacy
/// binary Excel workbook; no spreadsheet-parsing tool was available in this environment, so its
/// content was reconstructed from raw string extraction rather than reading the actual cell grid.
/// That extraction confirms the three record types (R00/R01/R02), the item-type codes (0/1/2) and
/// several field concepts (worker internal number, dd.MM.yyyy dates, center/order/operation codes),
/// but NOT the exact column order, delimiter or field widths. The semicolon delimiter and field order
/// below are a best-effort, clearly provisional reconstruction and must be verified against a real
/// KROS OMEGA-exported sample before production use — this formatter has materially lower confidence
/// than the project's other country-pack formatters. Klacks has no chart-of-accounts or Kros
/// center/order/operation codes, so those fields are left empty; the worker internal number is the
/// employee's personnel number, mirroring the placeholder-account approach used elsewhere.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class OmegaSkExportFormatter : IExportFormatter
{
    private const string Delimiter = ";";
    private const string DateFormat = "dd.MM.yyyy";
    private const int Windows1250CodePage = 1250;
    private const string RecordTypeFormatHeader = "R00";
    private const string RecordTypeDocumentHeader = "R01";
    private const string RecordTypeDocumentItem = "R02";
    private const string OmegaFormatVersion = "1.0";
    private const string ItemTypeAccountingEntry = "0";
    private const string ItemTypeReimbursement = "1";
    private const string ItemTypeReduction = "2";

    public string FormatKey => ExportConstants.FormatOmegaSk;

    public string ContentType => ExportConstants.ContentTypeOctetStream;

    public string FileExtension => ".txt";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var currencyCode = string.IsNullOrWhiteSpace(options.CurrencyCode) ? ExportConstants.DefaultCurrencyCode : options.CurrencyCode;
        var documentNumber = 1;

        sb.Append(string.Join(Delimiter, RecordTypeFormatHeader, "OMEGA", OmegaFormatVersion));
        sb.Append(Environment.NewLine);

        foreach (var order in data.Orders)
        {
            if (order.WorkEntries.Count == 0)
            {
                continue;
            }

            var documentDate = order.WorkEntries[0].WorkDate;
            var totalAmount = order.WorkEntries.Sum(w =>
                w.WorkTime + w.Surcharges
                + w.Changes.Sum(c => c.ChangeTime + c.Surcharges)
                + w.Expenses.Sum(e => e.Amount));

            sb.Append(string.Join(
                Delimiter,
                RecordTypeDocumentHeader,
                documentNumber.ToString(CultureInfo.InvariantCulture),
                documentDate.ToString(DateFormat, CultureInfo.InvariantCulture),
                Sanitize(order.CustomerName ?? order.OrderName),
                currencyCode,
                FormatAmount(totalAmount)));
            sb.Append(Environment.NewLine);

            foreach (var work in order.WorkEntries)
            {
                var workAmount = work.WorkTime + work.Surcharges
                    + work.Changes.Sum(c => c.ChangeTime + c.Surcharges);

                AppendItem(sb, ItemTypeAccountingEntry, work.EmployeeIdNumber, work.WorkDate, work.EmployeeName, workAmount);

                foreach (var expense in work.Expenses)
                {
                    var itemType = expense.Taxable ? ItemTypeReimbursement : ItemTypeReduction;
                    AppendItem(sb, itemType, work.EmployeeIdNumber, work.WorkDate, expense.Description, expense.Amount);
                }
            }

            documentNumber++;
        }

        return Encoding.GetEncoding(Windows1250CodePage).GetBytes(sb.ToString());
    }

    private static void AppendItem(
        StringBuilder sb,
        string itemType,
        int workerInternalNumber,
        DateOnly itemDate,
        string text,
        decimal amount)
    {
        sb.Append(string.Join(
            Delimiter,
            RecordTypeDocumentItem,
            itemType,
            workerInternalNumber.ToString(CultureInfo.InvariantCulture),
            itemDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            Sanitize(text),
            FormatAmount(amount)));
        sb.Append(Environment.NewLine);
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString("F2", CultureInfo.InvariantCulture);
    }

    private static string Sanitize(string value)
    {
        return value.Replace(Delimiter, string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}
