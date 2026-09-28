// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Application.DTOs.Settings;

public class StateResource
{
    public string Abbreviation { get; set; } = string.Empty;

    public string CountryPrefix { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public MultiLanguage Name { get; set; } = null!;
}
