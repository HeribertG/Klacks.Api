// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Turns a resolved report into an .xlsx workbook. The client sends the already resolved values,
/// because the data providers live in the frontend; this builder gives them their type back so the
/// spreadsheet can compute with numbers and sort by dates instead of treating everything as text.
/// </summary>
/// <remarks>
/// Grouped sheets get a real Excel outline plus a SUBTOTAL formula per group, so the subtotals stay
/// correct when the user filters or hides rows in Excel.
/// </remarks>
using System.Globalization;
using ClosedXML.Excel;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ReportXlsxBuilder : IReportXlsxBuilder
{
    private const string FileExtension = ".xlsx";
    private const string FallbackFileName = "report";
    private const string FallbackSheetName = "Report";
    private const string SubtotalLabel = "Σ";
    private const string NumberFormat = "#,##0.00";
    private const string DateFormat = "dd.mm.yyyy";
    private const string TimeFormat = "hh:mm";
    private const int SubtotalFunctionSum = 9;
    private const int MaxSheetNameLength = 31;
    private const int HeaderRow = 1;
    private const string GroupSeparatorApostrophe = "'";
    private const string GroupSeparatorSpace = " ";
    private const string GroupSeparatorNarrowSpace = " ";

    private static readonly char[] InvalidSheetNameChars = ['\\', '/', '*', '?', ':', '[', ']'];

    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "dd.MM.yyyy",
        "d.M.yyyy",
        "dd/MM/yyyy",
        "yyyy-MM-ddTHH:mm:ss",
    ];

    public ReportExportResult Build(ReportXlsxRequest request)
    {
        using var workbook = new XLWorkbook();
        var usedNames = new List<string>();

        foreach (var sheetResource in request.Sheets)
        {
            var sheet = workbook.Worksheets.Add(ResolveSheetName(sheetResource.Name, usedNames));
            WriteSheet(sheet, sheetResource);
        }

        if (!workbook.Worksheets.Any())
        {
            workbook.Worksheets.Add(FallbackSheetName);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return new ReportExportResult
        {
            FileContent = stream.ToArray(),
            ContentType = ExportConstants.ContentTypeXlsx,
            FileName = ResolveFileName(request.FileName),
        };
    }

    private void WriteSheet(IXLWorksheet sheet, ReportXlsxSheetResource resource)
    {
        for (var column = 0; column < resource.Columns.Count; column++)
        {
            sheet.Cell(HeaderRow, column + 1).Value = resource.Columns[column].Header;
        }

        sheet.Row(HeaderRow).Style.Font.Bold = true;
        sheet.SheetView.FreezeRows(HeaderRow);

        var row = HeaderRow + 1;
        var groupIndex = resource.GroupColumnIndex;

        if (groupIndex is null || groupIndex < 0 || groupIndex >= resource.Columns.Count)
        {
            row = WriteRows(sheet, resource, resource.Rows, row, outline: false);
        }
        else
        {
            row = WriteGroupedRows(sheet, resource, groupIndex.Value, row);
        }

        if (resource.Columns.Count > 0)
        {
            sheet.Range(HeaderRow, 1, Math.Max(HeaderRow, row - 1), resource.Columns.Count)
                .SetAutoFilter();
            sheet.Columns().AdjustToContents();
        }
    }

    private int WriteGroupedRows(IXLWorksheet sheet, ReportXlsxSheetResource resource, int groupIndex, int row)
    {
        foreach (var group in GroupRows(resource.Rows, groupIndex))
        {
            var firstDataRow = row;
            row = WriteRows(sheet, resource, group.Rows, row, outline: true);

            if (!resource.Subtotals)
            {
                continue;
            }

            WriteSubtotalRow(sheet, resource, groupIndex, group.Key, firstDataRow, row - 1, row);
            row++;
        }

        return row;
    }

    private static IEnumerable<(string Key, List<List<string>> Rows)> GroupRows(
        List<List<string>> rows,
        int groupIndex)
    {
        var groups = new List<(string Key, List<List<string>> Rows)>();
        var index = new Dictionary<string, int>();

        foreach (var row in rows)
        {
            var key = groupIndex < row.Count ? row[groupIndex] ?? string.Empty : string.Empty;
            if (index.TryGetValue(key, out var position))
            {
                groups[position].Rows.Add(row);
            }
            else
            {
                index[key] = groups.Count;
                groups.Add((key, [row]));
            }
        }

        return groups;
    }

    private int WriteRows(
        IXLWorksheet sheet,
        ReportXlsxSheetResource resource,
        List<List<string>> rows,
        int row,
        bool outline)
    {
        foreach (var values in rows)
        {
            for (var column = 0; column < resource.Columns.Count; column++)
            {
                var raw = column < values.Count ? values[column] : string.Empty;
                WriteCell(sheet.Cell(row, column + 1), raw, (ReportFieldType)resource.Columns[column].Type);
            }

            if (outline)
            {
                sheet.Row(row).OutlineLevel = 1;
            }

            row++;
        }

        return row;
    }

    private static void WriteSubtotalRow(
        IXLWorksheet sheet,
        ReportXlsxSheetResource resource,
        int groupIndex,
        string groupKey,
        int firstDataRow,
        int lastDataRow,
        int row)
    {
        sheet.Cell(row, groupIndex + 1).Value = $"{SubtotalLabel} {groupKey}";

        for (var column = 0; column < resource.Columns.Count; column++)
        {
            if (!IsNumeric((ReportFieldType)resource.Columns[column].Type))
            {
                continue;
            }

            var letter = sheet.Column(column + 1).ColumnLetter();
            var cell = sheet.Cell(row, column + 1);
            cell.FormulaA1 = $"SUBTOTAL({SubtotalFunctionSum},{letter}{firstDataRow}:{letter}{lastDataRow})";
            cell.Style.NumberFormat.Format = NumberFormat;
        }

        sheet.Row(row).Style.Font.Bold = true;
    }

    /// <summary>
    /// Writes a value in its declared type. A value that does not parse is written as text,
    /// so nothing is lost when the data does not match the column definition.
    /// </summary>
    private static void WriteCell(IXLCell cell, string raw, ReportFieldType type)
    {
        var value = (raw ?? string.Empty).Trim();
        if (value.Length == 0)
        {
            return;
        }

        switch (type)
        {
            case ReportFieldType.Number:
            case ReportFieldType.Currency:
                if (TryParseNumber(value, out var number))
                {
                    cell.Value = number;
                    cell.Style.NumberFormat.Format = NumberFormat;
                    return;
                }
                break;

            case ReportFieldType.Date:
                if (TryParseDate(value, out var date))
                {
                    cell.Value = date;
                    cell.Style.NumberFormat.Format = DateFormat;
                    return;
                }
                break;

            case ReportFieldType.Time:
                if (TimeSpan.TryParse(value, CultureInfo.InvariantCulture, out var time))
                {
                    cell.Value = time;
                    cell.Style.NumberFormat.Format = TimeFormat;
                    return;
                }
                break;

            case ReportFieldType.Boolean:
                if (bool.TryParse(value, out var flag))
                {
                    cell.Value = flag;
                    return;
                }
                break;

            default:
                break;
        }

        cell.Value = value;
    }

    private static bool IsNumeric(ReportFieldType type)
    {
        return type is ReportFieldType.Number or ReportFieldType.Currency;
    }

    /// <summary>
    /// Reads a number that was formatted for display before it was sent, in any of the separator
    /// conventions in use here.
    /// </summary>
    /// <remarks>
    /// Parsing with NumberStyles.Any against the invariant culture is not enough and actively
    /// harmful: it reads "1234,50" as 123450, because the comma passes as a thousands separator.
    /// The separators are therefore normalised first, and the resulting number is parsed without
    /// allowing group separators at all.
    /// </remarks>
    private static bool TryParseNumber(string value, out double result)
    {
        var stripped = value
            .Replace(GroupSeparatorApostrophe, string.Empty)
            .Replace(GroupSeparatorSpace, string.Empty)
            .Replace(GroupSeparatorNarrowSpace, string.Empty);

        var lastComma = stripped.LastIndexOf(',');
        var lastDot = stripped.LastIndexOf('.');

        string normalised;
        if (lastComma >= 0 && lastDot >= 0)
        {
            // Whichever separator comes last is the decimal one; the other groups the digits.
            normalised = lastComma > lastDot
                ? stripped.Replace(".", string.Empty).Replace(',', '.')
                : stripped.Replace(",", string.Empty);
        }
        else if (lastComma >= 0)
        {
            normalised = stripped.Replace(',', '.');
        }
        else
        {
            normalised = stripped;
        }

        return double.TryParse(normalised, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
    }

    private static bool TryParseDate(string value, out DateTime result)
    {
        return DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    private static string ResolveSheetName(string name, List<string> usedNames)
    {
        var cleaned = new string((name ?? string.Empty)
            .Where(c => !InvalidSheetNameChars.Contains(c))
            .ToArray())
            .Trim();

        if (cleaned.Length == 0)
        {
            cleaned = FallbackSheetName;
        }

        if (cleaned.Length > MaxSheetNameLength)
        {
            cleaned = cleaned[..MaxSheetNameLength];
        }

        var candidate = cleaned;
        var suffix = 2;
        while (usedNames.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            var marker = $" ({suffix++})";
            var keep = Math.Min(cleaned.Length, MaxSheetNameLength - marker.Length);
            candidate = cleaned[..keep] + marker;
        }

        usedNames.Add(candidate);
        return candidate;
    }

    private static string ResolveFileName(string name)
    {
        var cleaned = (name ?? string.Empty).Trim();
        if (cleaned.Length == 0)
        {
            cleaned = FallbackFileName;
        }

        return cleaned.EndsWith(FileExtension, StringComparison.OrdinalIgnoreCase)
            ? cleaned
            : cleaned + FileExtension;
    }
}
