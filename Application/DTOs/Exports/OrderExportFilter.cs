// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filter for order export requests.
/// @param OrderIds - Sealed order shift IDs to export (at least one required)
/// @param FromDate - Optional lower bound for Work.CurrentDate; only services performed on or after this day are exported
/// @param UntilDate - Optional upper bound for Work.CurrentDate
/// @param Format - Export format key (csv, json, xml, datev, bmd)
/// @param Language - Culture name for date/number formatting
/// @param CurrencyCode - ISO 4217 currency code carried into the export metadata
/// @param GroupId - Optional group filter retained for audit log compatibility
/// </summary>
namespace Klacks.Api.Application.DTOs.Exports;

public class OrderExportFilter
{
    public List<Guid> OrderIds { get; set; } = [];

    public DateOnly? FromDate { get; set; }

    public DateOnly? UntilDate { get; set; }

    public string Format { get; set; } = string.Empty;

    public string Language { get; set; } = "de";

    public string CurrencyCode { get; set; } = "EUR";

    public Guid? GroupId { get; set; }
}
