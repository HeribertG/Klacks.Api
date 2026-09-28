// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICountryResolver
{
    Task<Countries?> ResolveAsync(string? nameOrCode, CancellationToken ct = default);

    Task<Countries?> GetDefaultAsync(CancellationToken ct = default);
}
