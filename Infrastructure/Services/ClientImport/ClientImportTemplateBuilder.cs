// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Writes the employee import template as xlsx with ClosedXML: one sheet, a bold frozen header row,
/// and text format on every column so postcodes and phone numbers keep their leading zeros.
/// </summary>

using ClosedXML.Excel;
using Klacks.Api.Application.Interfaces.ClientImport;

namespace Klacks.Api.Infrastructure.Services.ClientImport;

public class ClientImportTemplateBuilder : IClientImportTemplateBuilder
{
    private const int HeaderRow = 1;
    private const string TextNumberFormat = "@";
    private const double MinColumnWidth = 14;
    private const int ColumnPadding = 4;

    public byte[] Build(string sheetName, IReadOnlyList<string> headers)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(sheetName);

        for (var index = 0; index < headers.Count; index++)
        {
            var column = sheet.Column(index + 1);
            column.Style.NumberFormat.Format = TextNumberFormat;
            column.Width = Math.Max(headers[index].Length + ColumnPadding, MinColumnWidth);

            var cell = sheet.Cell(HeaderRow, index + 1);
            cell.Value = headers[index];
            cell.Style.Font.Bold = true;
        }

        sheet.SheetView.FreezeRows(HeaderRow);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
