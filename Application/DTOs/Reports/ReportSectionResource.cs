// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportSectionResource
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    public float Height { get; set; }
    public List<ReportFieldResource> Fields { get; set; } = [];
    public bool Visible { get; set; } = true;
    public int SortOrder { get; set; }
    public string? BackgroundColor { get; set; }
    public List<ReportFieldResource>? TableFooterFields { get; set; }
    public List<FreeTextRowResource>? FreeTextRows { get; set; }
}
