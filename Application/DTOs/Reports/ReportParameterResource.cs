// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportParameterResource
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public int Type { get; set; }

    public bool Required { get; set; }

    public string? DefaultValue { get; set; }

    public List<string>? Choices { get; set; }

    public int BindsTo { get; set; }
}
