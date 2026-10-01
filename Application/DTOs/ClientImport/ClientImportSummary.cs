// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportSummary
{
    public int Total { get; set; }

    public int Ready { get; set; }

    public int Skipped { get; set; }

    public int Errors { get; set; }

    public int Duplicates { get; set; }

    public int Warnings { get; set; }
}
