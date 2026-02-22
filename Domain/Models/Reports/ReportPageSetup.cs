// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Reports;

public class ReportPageSetup
{
    public ReportOrientation Orientation { get; set; } = ReportOrientation.Portrait;
    public ReportPageSize Size { get; set; } = ReportPageSize.A4;
    public ReportMargins Margins { get; set; } = new();
}
