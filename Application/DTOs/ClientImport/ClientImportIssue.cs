// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportIssue
{
    public string? Field { get; set; }

    public ClientImportIssueSeverity Severity { get; set; }

    public string Code { get; set; } = string.Empty;

    public Dictionary<string, string> Args { get; set; } = [];
}
