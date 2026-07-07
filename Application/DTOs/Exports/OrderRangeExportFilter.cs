// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filter for date-range based ZIP order export requests.
/// @param FromDate - Lower bound (inclusive) for Work.CurrentDate; required
/// @param UntilDate - Upper bound (inclusive) for Work.CurrentDate; required, must not be before FromDate
/// @param Format - Export format key for the per-order files (csv, json, xml, datev, bmd)
/// @param Language - Culture name for date/number formatting
/// @param CurrencyCode - ISO 4217 currency code carried into the export metadata
/// </summary>
using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.DTOs.Exports;

public class OrderRangeExportFilter
{
    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public string Format { get; set; } = ExportConstants.FormatXml;

    public string Language { get; set; } = ExportConstants.DefaultLanguage;

    public string CurrencyCode { get; set; } = ExportConstants.DefaultCurrencyCode;
}
