// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportRequest
{
    public Guid Token { get; set; }

    public string? FileName { get; set; } = string.Empty;

    public List<ClientImportColumn> Columns { get; set; } = [];

    public List<List<string>> Rows { get; set; } = [];

    public List<ClientImportColumnMapping> Mapping { get; set; } = [];

    public ClientImportDateFormat DateFormat { get; set; }

    public ClientImportNameOrder NameOrder { get; set; }

    public ClientImportPolicy Policy { get; set; } = new();

    public List<ClientImportRowOverride> RowOverrides { get; set; } = [];
}
