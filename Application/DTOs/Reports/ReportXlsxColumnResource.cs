// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

/// <summary>
/// One column of a report sheet. The type decides how the values are written, so numbers
/// stay computable and dates stay sortable in the spreadsheet.
/// </summary>
public class ReportXlsxColumnResource
{
    public string Header { get; set; } = string.Empty;

    public int Type { get; set; }
}
