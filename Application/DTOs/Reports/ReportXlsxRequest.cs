// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

/// <summary>
/// Workbook request sent by the report designer. The client has already resolved every value,
/// because the data providers that know how to read a row live in the frontend; the server only
/// turns the resolved values into a typed spreadsheet.
/// </summary>
public class ReportXlsxRequest
{
    public string FileName { get; set; } = "report";

    public List<ReportXlsxSheetResource> Sheets { get; set; } = [];
}
