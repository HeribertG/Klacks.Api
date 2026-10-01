// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Raw cell grid of one sheet as a file reader returns it: every row including a possible title and the
/// header row, all cells as strings (dates as ISO yyyy-MM-dd). Delimiter and Encoding are only set by
/// the CSV reader.
/// </summary>

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportSheet
{
    public List<string> SheetNames { get; init; } = [];

    public string? SheetName { get; init; }

    public List<List<string>> Rows { get; init; } = [];

    public string? Delimiter { get; init; }

    public string? Encoding { get; init; }
}
