// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportCommitResult
{
    public Guid BatchId { get; set; }

    public int Created { get; set; }

    public int Skipped { get; set; }

    public int GeocodingQueued { get; set; }
}
