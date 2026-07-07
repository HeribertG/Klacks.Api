// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads the company registration details (name, tax id, VAT id, commercial register)
/// from the application settings for use in export documents.
/// </summary>
using Klacks.Api.Domain.Models.Exports;

namespace Klacks.Api.Application.Interfaces.Exports;

public interface ICompanyInfoLoader
{
    Task<CompanyInfo> LoadAsync(CancellationToken cancellationToken = default);
}
