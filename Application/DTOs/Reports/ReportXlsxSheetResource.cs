// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

/// <summary>
/// One table section of a report, becoming one worksheet.
/// </summary>
/// <param name="GroupColumnIndex">Column the rows are grouped by, zero based; null when ungrouped</param>
public class ReportXlsxSheetResource
{
    public string Name { get; set; } = "Sheet";

    public List<ReportXlsxColumnResource> Columns { get; set; } = [];

    public List<List<string>> Rows { get; set; } = [];

    public int? GroupColumnIndex { get; set; }

    public bool Subtotals { get; set; }
}
