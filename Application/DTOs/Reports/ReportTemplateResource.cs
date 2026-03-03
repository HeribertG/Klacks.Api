// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportTemplateResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Type { get; set; }
    public string SourceId { get; set; } = "schedule";
    public List<string> DataSetIds { get; set; } = ["work"];
    public ReportPageSetupResource PageSetup { get; set; } = new();
    public List<ReportSectionResource> Sections { get; set; } = [];
    public bool MergeRows { get; set; }
    public bool ShowFullPeriod { get; set; }
}
