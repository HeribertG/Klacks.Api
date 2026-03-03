// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportPageSetupResource
{
    public int Orientation { get; set; }
    public int Size { get; set; }
    public ReportMarginsResource Margins { get; set; } = new();
}
