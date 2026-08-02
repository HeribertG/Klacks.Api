// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Reports;

public class ReportTemplateVersion
{
    public DateTime SavedAt { get; set; }

    public string? SavedBy { get; set; }

    public string Name { get; set; } = string.Empty;

    public ReportPageSetup PageSetup { get; set; } = new();

    public List<ReportSection> Sections { get; set; } = [];
}
