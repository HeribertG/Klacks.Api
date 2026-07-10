// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Describes an available export format (order/Fibu family or payroll/country-pack family)
/// for the settings selection UI.
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class ExportFormatResource
{
    public string Key { get; set; } = string.Empty;

    public bool Fixed { get; set; }

    public bool Enabled { get; set; }
}
