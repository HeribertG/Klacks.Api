// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the company registration details from the settings store, shared by the
/// export handlers so the settings keys are resolved in a single place.
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class CompanyInfoLoader : ICompanyInfoLoader
{
    private readonly ISettingsReader _settingsReader;

    public CompanyInfoLoader(ISettingsReader settingsReader)
    {
        _settingsReader = settingsReader;
    }

    public async Task<CompanyInfo> LoadAsync(CancellationToken cancellationToken = default)
    {
        var nameSetting = await _settingsReader.GetSetting(Application.Constants.Settings.APP_ADDRESS_NAME);
        var taxIdSetting = await _settingsReader.GetSetting(Application.Constants.Settings.COMPANY_TAX_ID);
        var vatIdSetting = await _settingsReader.GetSetting(Application.Constants.Settings.COMPANY_VAT_ID);
        var commercialRegisterSetting = await _settingsReader.GetSetting(Application.Constants.Settings.COMPANY_COMMERCIAL_REGISTER);

        return new CompanyInfo
        {
            Name = nameSetting?.Value ?? string.Empty,
            TaxId = taxIdSetting?.Value ?? string.Empty,
            VatId = vatIdSetting?.Value ?? string.Empty,
            CommercialRegister = commercialRegisterSetting?.Value ?? string.Empty
        };
    }
}
