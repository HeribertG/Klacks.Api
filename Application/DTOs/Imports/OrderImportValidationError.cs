// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Imports;

/// <summary>
/// One problem the order import parser found.
/// </summary>
/// <param name="OrderIndex">Zero-based position of the rejected order element in the file; null for a file-level
/// problem (malformed XML, wrong root, schema version). All errors of one order share the same index, so rejected
/// orders can be counted one by one even when they carry no (or a repeated) order reference.</param>
public class OrderImportValidationError
{
    public int? LineNumber { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? ExternalOrderReference { get; set; }

    public int? OrderIndex { get; set; }
}
