// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads an uploaded employee list (xlsx or csv) into the raw grid the import wizard works on and
/// proposes the column mapping, date order and name order. Nothing is stored: the grid goes back to the
/// browser, and the returned token only makes the later commit idempotent. The grid is checked against
/// the same character budget the request guard applies when it comes back.
/// </summary>
/// <param name="readers">File readers; the first that accepts the file extension reads it</param>
/// <param name="detector">Proposes a target per column</param>
/// <param name="companyClock">Company "today" (current year for two-digit years and age heuristics)</param>

using Klacks.Api.Application.Commands.ClientImport;
using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces.ClientImport;
using Klacks.Api.Application.Services.ClientImport;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ClientImport;

public class ParseClientImportCommandHandler : BaseHandler, IRequestHandler<ParseClientImportCommand, ClientImportParseResult>
{
    private static readonly ClientImportTarget[] DateTargets =
        [ClientImportTarget.Birthdate, ClientImportTarget.EntryDate, ClientImportTarget.ExitDate];

    private readonly IEnumerable<IClientImportFileReader> _readers;
    private readonly ClientImportColumnDetector _detector;
    private readonly ICompanyClock _companyClock;

    public ParseClientImportCommandHandler(
        IEnumerable<IClientImportFileReader> readers,
        ClientImportColumnDetector detector,
        ICompanyClock companyClock,
        ILogger<ParseClientImportCommandHandler> logger)
        : base(logger)
    {
        _readers = readers;
        _detector = detector;
        _companyClock = companyClock;
    }

    public async Task<ClientImportParseResult> Handle(ParseClientImportCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            if (request.Content.LongLength > ClientImportLimits.MaxFileBytes)
            {
                throw new ClientImportRejectedException(ClientImportErrorCodes.FileTooLarge, $"The file exceeds {ClientImportLimits.MaxFileBytes} bytes.");
            }

            if (request.Content.Length == 0)
            {
                throw new ClientImportRejectedException(ClientImportErrorCodes.FileEmpty, "The file is empty.");
            }

            var reader = _readers.FirstOrDefault(r => r.CanRead(request.FileName))
                ?? throw new ClientImportRejectedException(ClientImportErrorCodes.FileUnsupported, "Only .xlsx, .csv and .txt files can be imported.");

            var sheet = reader.Read(request.Content, request.SheetName);
            var today = await _companyClock.GetTodayAsync(cancellationToken);
            var result = BuildResult(sheet, request.FileName, today.Year);

            _logger.LogInformation("Client import parsed: {Rows} rows, {Columns} columns", result.Rows.Count, result.Columns.Count);
            return result;
        },
        "parsing client import file");
    }

    private ClientImportParseResult BuildResult(ClientImportSheet sheet, string fileName, int currentYear)
    {
        var rows = sheet.Rows.Where(row => row.Any(cell => !string.IsNullOrWhiteSpace(cell))).ToList();
        if (rows.Count == 0)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileEmpty, "The file contains no data.");
        }

        var headerIndex = ClientImportHeaderRowLocator.Locate(rows)
            ?? throw new ClientImportRejectedException(ClientImportErrorCodes.NoHeaderRow, "No header row was found in the first rows.");

        var columnCount = rows.Max(row => row.Count);
        if (columnCount > ClientImportLimits.MaxColumns)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.TooManyColumns, $"The file has {columnCount} columns, more than {ClientImportLimits.MaxColumns}.");
        }

        var dataRows = rows.Skip(headerIndex + 1).Select(row => Pad(row, columnCount)).ToList();
        if (dataRows.Count == 0)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileEmpty, "The file has a header but no data rows.");
        }

        if (dataRows.Count > ClientImportLimits.MaxRows)
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.TooManyRows, $"The file has more than {ClientImportLimits.MaxRows} data rows.");
        }

        if (ClientImportGridBudget.Exceeds(dataRows))
        {
            throw new ClientImportRejectedException(ClientImportErrorCodes.FileTooLarge, $"The file holds more than {ClientImportLimits.MaxGridChars} characters.");
        }

        var headers = Pad(rows[headerIndex], columnCount).Select(h => h.Trim()).ToList();
        var mapping = _detector.Detect(headers, dataRows, currentYear);
        var (dateFormat, ambiguous) = ClientImportDateFormatDetector.Detect(ValuesOf(mapping, dataRows, DateTargets));

        return new ClientImportParseResult
        {
            Token = Guid.NewGuid(),
            FileName = ClientImportFileName.Sanitize(fileName),
            Sheets = sheet.SheetNames,
            SheetName = sheet.SheetName,
            HeaderRowIndex = headerIndex,
            Columns = headers.Select((header, index) => new ClientImportColumn
            {
                Index = index,
                Header = header,
                Samples = dataRows.Select(row => row[index].Trim()).Where(v => v.Length > 0).Take(ClientImportLimits.MaxSamples).ToList()
            }).ToList(),
            Rows = dataRows,
            Mapping = mapping,
            DateFormat = dateFormat,
            DateFormatAmbiguous = ambiguous,
            NameOrder = _detector.DetectNameOrder(headers, mapping, dataRows),
            Delimiter = sheet.Delimiter,
            Encoding = sheet.Encoding
        };
    }

    private static IEnumerable<string> ValuesOf(List<ClientImportColumnMapping> mapping, List<List<string>> rows, ClientImportTarget[] targets)
    {
        var columns = mapping.Where(m => targets.Contains(m.Target)).Select(m => m.ColumnIndex).ToList();
        return columns.SelectMany(column => rows.Select(row => row[column])).Where(v => !string.IsNullOrWhiteSpace(v));
    }

    private static List<string> Pad(List<string> row, int columnCount)
    {
        var padded = new List<string>(columnCount);
        for (var index = 0; index < columnCount; index++)
        {
            var cell = index < row.Count ? row[index] ?? string.Empty : string.Empty;
            padded.Add(cell.Length > ClientImportLimits.MaxCellLength ? cell[..ClientImportLimits.MaxCellLength] : cell);
        }

        return padded;
    }
}
