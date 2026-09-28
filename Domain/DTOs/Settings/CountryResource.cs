// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.DTOs.Settings;

public class CountryResource
{
    public string Abbreviation { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public MultiLanguage Name { get; set; } = null!;

    public string Prefix { get; set; } = string.Empty;

    public bool Select { get; set; }
}
