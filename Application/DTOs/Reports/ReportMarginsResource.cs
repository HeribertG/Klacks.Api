// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportMarginsResource
{
    public float Top { get; set; } = ReportDefaults.Margin;
    public float Bottom { get; set; } = ReportDefaults.Margin;
    public float Left { get; set; } = ReportDefaults.Margin;
    public float Right { get; set; } = ReportDefaults.Margin;
}
