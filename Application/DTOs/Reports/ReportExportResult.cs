// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportExportResult
{
    public byte[] FileContent { get; set; } = [];

    public string ContentType { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
}
