// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportTemplateVersionResource
{
    public DateTime SavedAt { get; set; }

    public string? SavedBy { get; set; }

    public string Name { get; set; } = string.Empty;

    public ReportPageSetupResource PageSetup { get; set; } = new();

    public List<ReportSectionResource> Sections { get; set; } = [];
}
