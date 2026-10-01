// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportColumn
{
    public int Index { get; set; }

    public string? Header { get; set; } = string.Empty;

    public List<string> Samples { get; set; } = [];
}
