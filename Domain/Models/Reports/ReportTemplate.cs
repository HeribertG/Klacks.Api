// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Reports;

public class ReportTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ReportType Type { get; set; }
    public string SourceId { get; set; } = "schedule";
    public List<string> DataSetIds { get; set; } = ["work"];
    public ReportPageSetup PageSetup { get; set; } = new();
    public List<ReportSection> Sections { get; set; } = [];
    public bool MergeRows { get; set; }
    public bool ShowFullPeriod { get; set; }

    public List<ReportTemplateVersion> Versions { get; set; } = [];

    public List<ReportParameter> Parameters { get; set; } = [];
}
