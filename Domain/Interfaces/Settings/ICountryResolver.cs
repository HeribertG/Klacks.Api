// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Settings;

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICountryResolver
{
    Task<Countries?> ResolveAsync(string? nameOrCode, CancellationToken ct = default);

    Task<Countries?> GetDefaultAsync(CancellationToken ct = default);
}
