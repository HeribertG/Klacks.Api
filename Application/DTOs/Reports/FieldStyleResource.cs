// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.DTOs.Reports;

public class FieldStyleResource
{
    public string FontFamily { get; set; } = "Arial";
    public float FontSize { get; set; } = ReportDefaults.FontSize;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public bool Underline { get; set; }
    public string TextColor { get; set; } = "#000000";
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public int Alignment { get; set; }
    public CellBorderStyleResource? CellBorder { get; set; }
}
