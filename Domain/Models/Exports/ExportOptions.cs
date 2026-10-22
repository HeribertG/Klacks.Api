// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Options for controlling export output format and localization.
/// @param Language - ISO language code (de, en, fr, it)
/// @param CurrencyCode - ISO 4217 currency code (EUR, USD, CHF)
/// @param CompanyInfo - Company registration details for invoice formats (ZUGFeRD)
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Models.Exports;

public class ExportOptions
{
    public string Language { get; set; } = "de";

    public string CurrencyCode { get; set; } = "EUR";

    public string DateFormat { get; set; } = DateTimeFormats.DottedDate;

    public string TimeFormat { get; set; } = DateTimeFormats.HourMinute;

    public CompanyInfo Company { get; set; } = new();
}
