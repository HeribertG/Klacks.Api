// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the trimmed cell of one import row for a target; an empty or whitespace cell and an unmapped
/// target both read as null.
/// </summary>
/// <param name="cells">Cells of the row</param>
/// <param name="columns">Column index per mapped target</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportRowReader
{
    private readonly IReadOnlyList<string> _cells;
    private readonly ClientImportColumnMap _columns;

    public ClientImportRowReader(IReadOnlyList<string> cells, ClientImportColumnMap columns)
    {
        _cells = cells;
        _columns = columns;
    }

    public string? Value(ClientImportTarget target)
    {
        if (!_columns.TryGetColumn(target, out var column) || column >= _cells.Count)
        {
            return null;
        }

        var value = _cells[column]?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }
}
