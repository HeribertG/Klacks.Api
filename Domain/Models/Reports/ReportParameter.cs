// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Reports;

public class ReportParameter
{
    public string Key { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public ReportParameterType Type { get; set; } = ReportParameterType.Text;

    public bool Required { get; set; }

    public string? DefaultValue { get; set; }

    public List<string>? Choices { get; set; }

    public ReportParameterBinding BindsTo { get; set; } = ReportParameterBinding.None;
}
