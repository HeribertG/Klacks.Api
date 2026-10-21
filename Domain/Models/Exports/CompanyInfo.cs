// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Exports;

public class CompanyInfo
{
    public string Name { get; set; } = string.Empty;

    public string TaxId { get; set; } = string.Empty;

    public string VatId { get; set; } = string.Empty;

    public string CommercialRegister { get; set; } = string.Empty;
}
