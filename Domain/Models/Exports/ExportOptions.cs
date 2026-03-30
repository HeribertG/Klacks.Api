// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Options for controlling export output format and localization.
/// @param Language - ISO language code (de, en, fr, it)
/// @param CurrencyCode - ISO 4217 currency code (EUR, USD, CHF)
/// @param CompanyInfo - Company registration details for invoice formats (ZUGFeRD)
/// </summary>
namespace Klacks.Api.Domain.Models.Exports;

public class ExportOptions
{
    public string Language { get; set; } = "de";

    public string CurrencyCode { get; set; } = "EUR";

    public string DateFormat { get; set; } = "dd.MM.yyyy";

    public string TimeFormat { get; set; } = "HH:mm";

    public CompanyInfo Company { get; set; } = new();
}

public class CompanyInfo
{
    public string Name { get; set; } = string.Empty;

    public string TaxId { get; set; } = string.Empty;

    public string VatId { get; set; } = string.Empty;

    public string CommercialRegister { get; set; } = string.Empty;
}
