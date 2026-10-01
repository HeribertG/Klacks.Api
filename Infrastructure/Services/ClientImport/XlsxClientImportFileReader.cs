// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads an xlsx employee list with ClosedXML after the zip-bomb check. The requested sheet (or the
/// first visible one) is returned as strings: real date cells as ISO yyyy-MM-dd, whole numbers in the
/// General format without exponent or decimals (phone numbers, postcodes), other numbers as displayed.
/// Formula cells are read from their cached result (what Excel, LibreOffice and Google Sheets store) and
/// never evaluated, so a crafted formula cannot cost unbounded time; a formula without a stored result
/// reads as empty. The characters read are counted while reading (repeated shared strings unpack to far
/// more text than the file size) and a sheet above ClientImportLimits.MaxGridChars is rejected. A
/// ClosedXML failure is rejected with a generic message; its details stay in the log.
/// </summary>

using System.Globalization;
using ClosedXML.Excel;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Services.ClientImport;

public class XlsxClientImportFileReader : IClientImportFileReader
{
    private const string Extension = ".xlsx";
    private const string IntegerFormat = "0";
    private const int GeneralNumberFormatId = 0;

    public bool CanRead(string fileName) =>
        string.Equals(Path.GetExtension(fileName), Extension, StringComparison.OrdinalIgnoreCase);

    public ClientImportSheet Read(byte[] content, string? sheetName)
    {
        ClientImportZipGuard.EnsureWithinLimits(content, ClientImportLimits.MaxUnpackedBytes, ClientImportLimits.MaxZipEntries);

        try
        {
            using var stream = new MemoryStream(content, writable: false);
            using var workbook = new XLWorkbook(stream);
            return ReadWorkbook(workbook, sheetName);
        }
        catch (ClientImportRejectedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileUnsupported, "The xlsx file could not be read.", ex);
        }
    }

    private static ClientImportSheet ReadWorkbook(XLWorkbook workbook, string? sheetName)
    {
        var visibleSheets = workbook.Worksheets
            .Where(sheet => sheet.Visibility == XLWorksheetVisibility.Visible)
            .ToList();

        if (visibleSheets.Count == 0)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileEmpty, "The workbook has no visible sheet.");
        }

        var sheet = string.IsNullOrWhiteSpace(sheetName)
            ? visibleSheets[0]
            : visibleSheets.FirstOrDefault(s => string.Equals(s.Name, sheetName, StringComparison.Ordinal))
              ?? throw new ClientImportRejectedException(ClientImportErrorCodes.InvalidRequest, $"The sheet '{sheetName}' does not exist.");

        return new ClientImportSheet
        {
            SheetNames = visibleSheets.Select(s => s.Name).ToList(),
            SheetName = sheet.Name,
            Rows = ReadRows(sheet)
        };
    }

    private static List<List<string>> ReadRows(IXLWorksheet sheet)
    {
        var rows = new List<List<string>>();
        var lastColumn = sheet.LastColumnUsed(XLCellsUsedOptions.Contents)?.ColumnNumber() ?? 0;

        if (lastColumn > ClientImportLimits.MaxColumns)
        {
            throw new ClientImportRejectedException(
                ClientImportErrorCodes.TooManyColumns, $"The sheet uses {lastColumn} columns, more than {ClientImportLimits.MaxColumns}.");
        }

        var maxRecords = ClientImportLimits.MaxRows + ClientImportLimits.HeaderSearchRows + 1;
        long totalChars = 0;

        foreach (var usedRow in sheet.RowsUsed(XLCellsUsedOptions.Contents))
        {
            var row = new List<string>(lastColumn);
            for (var columnNumber = 1; columnNumber <= lastColumn; columnNumber++)
            {
                var cell = ReadCell(usedRow.Cell(columnNumber));
                totalChars += cell.Length;
                if (totalChars > ClientImportLimits.MaxGridChars)
                {
                    throw new ClientImportRejectedException(
                        ClientImportErrorCodes.FileTooLarge, $"The sheet holds more than {ClientImportLimits.MaxGridChars} characters.");
                }

                row.Add(cell);
            }

            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            rows.Add(row);
            if (rows.Count > maxRecords)
            {
                break;
            }
        }

        return rows;
    }

    private static string ReadCell(IXLCell cell)
    {
        var value = cell.CachedValue;

        if (value.IsBlank || value.IsError)
        {
            return string.Empty;
        }

        if (value.IsDateTime)
        {
            return ClientImportDateParser.Format(value.GetDateTime());
        }

        if (value.IsText)
        {
            return value.GetText();
        }

        if (value.IsNumber)
        {
            return FormatNumber(cell, value.GetNumber());
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatNumber(IXLCell cell, double number)
    {
        var numberFormat = cell.Style.NumberFormat;
        var isGeneral = numberFormat.NumberFormatId == GeneralNumberFormatId && string.IsNullOrEmpty(numberFormat.Format);

        if (isGeneral && number == Math.Floor(number) && Math.Abs(number) < long.MaxValue)
        {
            return number.ToString(IntegerFormat, CultureInfo.InvariantCulture);
        }

        return cell.HasFormula
            ? number.ToString(CultureInfo.InvariantCulture)
            : cell.GetFormattedString(CultureInfo.InvariantCulture);
    }
}
