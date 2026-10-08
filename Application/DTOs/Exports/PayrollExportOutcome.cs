// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Result of a payroll export run (or of re-downloading a stored one): the file and what it contained.
/// @param FileContent - The generated file as byte array
/// @param FileName - Suggested file name with extension
/// @param ContentType - MIME type for the HTTP response
/// @param SkippedEntryCount - Entries the formatter could not write; sent as a response header
/// @param AbsenceMappingInvalid - True when the absence mapping could not be parsed
/// @param PersonCount - Persons contained in the file
/// @param IsSupplementary - True when the file re-exports persons that had been exported for the period before
/// @param ExportLogId - The ExportLog row of the run; the stored artifact can be downloaded again through it
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class PayrollExportOutcome
{
    public byte[] FileContent { get; set; } = [];

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public int SkippedEntryCount { get; set; }

    public bool AbsenceMappingInvalid { get; set; }

    public int PersonCount { get; set; }

    public bool IsSupplementary { get; set; }

    public Guid ExportLogId { get; set; }
}
