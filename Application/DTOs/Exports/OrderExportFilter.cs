// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filter for order export requests.
/// @param StartDate - Start of the export period
/// @param EndDate - End of the export period
/// @param Format - Export format key (csv, json, xml, datev, etc.)
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class OrderExportFilter
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public string Format { get; set; } = string.Empty;

    public string Language { get; set; } = "de";

    public string CurrencyCode { get; set; } = "EUR";
}
