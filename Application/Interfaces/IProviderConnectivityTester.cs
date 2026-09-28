// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces;

public interface IProviderConnectivityTester
{
    Task<ProviderConnectivityStatus> TestAsync(
        string baseUrl,
        string? apiKey = null,
        CancellationToken ct = default);
}
