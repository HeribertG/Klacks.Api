// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces;

public interface IProviderWebDiscovery
{
    Task<List<ProviderCandidateResource>> DiscoverAsync(CancellationToken ct = default);
}
