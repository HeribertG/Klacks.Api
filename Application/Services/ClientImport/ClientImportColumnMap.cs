// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Column index per import target of a validated mapping; Ignore and unmapped targets are absent.
/// </summary>

using Klacks.Api.Application.DTOs.ClientImport;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.ClientImport;

public class ClientImportColumnMap
{
    private readonly Dictionary<ClientImportTarget, int> _columns;

    public ClientImportColumnMap(IEnumerable<ClientImportColumnMapping> mapping)
    {
        _columns = mapping
            .Where(m => m.Target != ClientImportTarget.Ignore)
            .ToDictionary(m => m.Target, m => m.ColumnIndex);
    }

    public bool TryGetColumn(ClientImportTarget target, out int column) => _columns.TryGetValue(target, out column);

    public bool Contains(ClientImportTarget target) => _columns.ContainsKey(target);
}
