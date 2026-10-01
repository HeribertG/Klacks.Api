// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.ClientImport;

public class ClientImportColumnMapping
{
    public int ColumnIndex { get; set; }

    public ClientImportTarget Target { get; set; }

    public double Confidence { get; set; }
}
