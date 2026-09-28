// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Imports;

public class OrderImportValidationError
{
    public int? LineNumber { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ExternalOrderReference { get; set; }
}
