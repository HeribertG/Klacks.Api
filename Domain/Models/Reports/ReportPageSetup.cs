// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Reports;

public class ReportPageSetup
{
    public ReportOrientation Orientation { get; set; } = ReportOrientation.Portrait;
    public ReportPageSize Size { get; set; } = ReportPageSize.A4;
    public ReportMargins Margins { get; set; } = new();
}
