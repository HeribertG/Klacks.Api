// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportParseResult
{
    public Guid Token { get; set; }

    public string FileName { get; set; } = string.Empty;

    public List<string> Sheets { get; set; } = [];

    public string? SheetName { get; set; }

    public int HeaderRowIndex { get; set; }

    public List<ClientImportColumn> Columns { get; set; } = [];

    public List<List<string>> Rows { get; set; } = [];

    public List<ClientImportColumnMapping> Mapping { get; set; } = [];

    public ClientImportDateFormat DateFormat { get; set; }

    public bool DateFormatAmbiguous { get; set; }

    public ClientImportNameOrder NameOrder { get; set; }

    public string? Delimiter { get; set; }

    public string? Encoding { get; set; }
}
