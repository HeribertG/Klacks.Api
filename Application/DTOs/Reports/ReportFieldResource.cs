// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.DTOs.Reports;

public class ReportFieldResource
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DataBinding { get; set; } = string.Empty;
    public int Type { get; set; }
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = ReportDefaults.FieldWidth;
    public float Height { get; set; } = ReportDefaults.FieldHeight;
    public FieldStyleResource Style { get; set; } = new();
    public string? Format { get; set; }
    public string? Formula { get; set; }
    public int SortOrder { get; set; }
    public string? ImageUrl { get; set; }
    public bool HideLabel { get; set; }
    public List<string>? AdditionalBindings { get; set; }
    public string? BindingSeparator { get; set; }
    public string? SortDirection { get; set; }

    public List<StyleConditionResource>? StyleConditions { get; set; }
}
