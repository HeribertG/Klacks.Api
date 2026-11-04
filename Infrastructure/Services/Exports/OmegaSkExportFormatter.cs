// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Exports order data as a KROS OMEGA text import of accounting documents (Slovakia, data type T00 "EUD").
/// The file is tab-separated, Windows-1250 encoded, CRLF terminated and uses three record types:
/// R00 (data type header, once: "R00", "T00"), R01 (document header, one per order) and R02 with item type 0
/// (accounting entry, one per work entry). Each record is written at the column positions
/// of the KROS field specification; columns Klacks has no data for stay empty. Expenses and allowances belong to
/// the employee, not the customer, and are therefore neither written as items nor added to the document total.
/// </summary>
/// <remarks>
/// Source: docs/knowledge/export-samples/sk-kros-omega-import-fieldspec.xls (KROS, Import_24_60.xls), sheet
/// "Vseobecne - General description" (first row R00, TAB separator, CRLF, Windows ANSI, dates DD.MM.YYYY) and
/// sheet "EUD" (column grid of R00/T00, R01 and R02 item type 0). R01 fills internal number (E), partner name (G),
/// date of issue (J), currency (O) and total amount in domestic currency (T). R02 fills item type (B), amount in
/// domestic currency (G), item text (I) and worker internal number (Y, the employee's personnel number).
/// Document type (R01 B), tally and sequence codes (R01 C/D, which must exist in OMEGA), VAT fields and the
/// MD/DAL accounts (R02 C-F) are not known to Klacks and stay empty, so OMEGA cannot book the file without
/// completing them.
/// </remarks>
using System.Globalization;
using System.Text;
using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Exports;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class OmegaSkExportFormatter : IExportFormatter
{
    private const string Delimiter = "\t";
    private const int Windows1250CodePage = 1250;
    private const string AmountFormat = "F2";
    private const string RecordTypeDataType = "R00";
    private const string RecordTypeDocumentHeader = "R01";
    private const string RecordTypeDocumentItem = "R02";
    private const string DataTypeAccountingDocuments = "T00";
    private const string ItemTypeAccountingEntry = "0";

    private const int DocumentHeaderColumnCount = 29;
    private const int DocumentHeaderInternalNumberIndex = 4;
    private const int DocumentHeaderPartnerNameIndex = 6;
    private const int DocumentHeaderIssueDateIndex = 9;
    private const int DocumentHeaderCurrencyIndex = 14;
    private const int DocumentHeaderTotalDomesticIndex = 19;

    private const int ItemColumnCount = 25;
    private const int ItemTypeIndex = 1;
    private const int ItemAmountDomesticIndex = 6;
    private const int ItemTextIndex = 8;
    private const int ItemWorkerInternalNumberIndex = 24;

    public string FormatKey => ExportConstants.FormatOmegaSk;

    public string ContentType => ExportConstants.ContentTypeOctetStream;

    public string FileExtension => ".txt";

    public byte[] Format(OrderExportData data, ExportOptions options)
    {
        var sb = new StringBuilder();
        var currencyCode = string.IsNullOrWhiteSpace(options.CurrencyCode) ? ExportConstants.DefaultCurrencyCode : options.CurrencyCode;
        var documentNumber = 1;

        AppendRecord(sb, [RecordTypeDataType, DataTypeAccountingDocuments]);

        foreach (var order in data.Orders)
        {
            if (order.WorkEntries.Count == 0)
            {
                continue;
            }

            var documentDate = order.WorkEntries[0].WorkDate;
            var totalAmount = order.WorkEntries.Sum(w => OrderWorkHours.BillableHours(w));

            var header = CreateRecord(DocumentHeaderColumnCount, RecordTypeDocumentHeader);
            header[DocumentHeaderInternalNumberIndex] = documentNumber.ToString(CultureInfo.InvariantCulture);
            header[DocumentHeaderPartnerNameIndex] = Sanitize(order.CustomerName ?? order.OrderName);
            header[DocumentHeaderIssueDateIndex] = documentDate.ToString(DateTimeFormats.DottedDate, CultureInfo.InvariantCulture);
            header[DocumentHeaderCurrencyIndex] = currencyCode;
            header[DocumentHeaderTotalDomesticIndex] = FormatAmount(totalAmount);
            AppendRecord(sb, header);

            foreach (var work in order.WorkEntries)
            {
                var workAmount = OrderWorkHours.BillableHours(work);

                AppendItem(sb, work.EmployeeIdNumber, work.EmployeeName, workAmount);
            }

            documentNumber++;
        }

        return Encoding.GetEncoding(Windows1250CodePage).GetBytes(sb.ToString());
    }

    private static void AppendItem(StringBuilder sb, int workerInternalNumber, string text, decimal amount)
    {
        var item = CreateRecord(ItemColumnCount, RecordTypeDocumentItem);
        item[ItemTypeIndex] = ItemTypeAccountingEntry;
        item[ItemAmountDomesticIndex] = FormatAmount(amount);
        item[ItemTextIndex] = Sanitize(text);
        item[ItemWorkerInternalNumberIndex] = workerInternalNumber.ToString(CultureInfo.InvariantCulture);
        AppendRecord(sb, item);
    }

    private static string[] CreateRecord(int columnCount, string recordType)
    {
        var record = new string[columnCount];
        Array.Fill(record, string.Empty);
        record[0] = recordType;
        return record;
    }

    private static void AppendRecord(StringBuilder sb, string[] fields)
    {
        sb.Append(string.Join(Delimiter, fields));
        sb.Append(ExportConstants.LineEnding);
    }

    private static string FormatAmount(decimal amount)
    {
        return amount.ToString(AmountFormat, CultureInfo.InvariantCulture);
    }

    private static string Sanitize(string value)
    {
        return value.Replace(Delimiter, string.Empty).Replace("\r", string.Empty).Replace("\n", string.Empty);
    }
}
