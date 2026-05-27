// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces;

public interface IProviderConnectivityTester
{
    Task<ProviderConnectivityStatus> TestAsync(
        string baseUrl,
        string? apiKey = null,
        CancellationToken ct = default);
}
