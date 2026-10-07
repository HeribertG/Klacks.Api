// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of an order export operation containing the file content.
/// @param FileContent - The generated file as byte array
/// @param FileName - Suggested file name with extension
/// @param ContentType - MIME type for the HTTP response
/// @param SkippedEntryCount - Entries the formatter could not write (payroll exports); sent as a response header
/// @param AbsenceMappingInvalid - True when the group's absence mapping could not be parsed (payroll exports)
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class OrderExportResult
{
    public byte[] FileContent { get; set; } = [];

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public int SkippedEntryCount { get; set; }

    public bool AbsenceMappingInvalid { get; set; }
}
