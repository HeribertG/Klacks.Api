// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces;

public interface IProviderWebDiscovery
{
    Task<List<ProviderCandidateResource>> DiscoverAsync(CancellationToken ct = default);
}
