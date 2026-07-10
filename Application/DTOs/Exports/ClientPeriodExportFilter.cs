// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Filter for client period export requests.
/// @param FromDate - Lower bound for Work.CurrentDate; only services performed on or after this day are exported
/// @param UntilDate - Upper bound for Work.CurrentDate
/// @param Language - Culture name for date/number formatting
/// @param CurrencyCode - ISO 4217 currency code carried into the export metadata
/// @param Format - Generic output format key (xml/csv/json); selects the client period formatter
/// </summary>
using Klacks.Api.Application.Constants;

namespace Klacks.Api.Application.DTOs.Exports;

public class ClientPeriodExportFilter
{
    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }

    public string Language { get; set; } = "de";

    public string CurrencyCode { get; set; } = "EUR";

    public string Format { get; set; } = ExportConstants.FormatXml;
}
