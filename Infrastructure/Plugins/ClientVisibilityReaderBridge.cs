// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bridges the Contracts IClientVisibilityReader to the host's IClientVisibilityGuard, so plugins apply the
/// same group-visibility boundary as the core handlers instead of a parallel rule.
/// </summary>
/// <param name="clientVisibilityGuard">The host's single source of truth for client visibility</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public class ClientVisibilityReaderBridge : IClientVisibilityReader
{
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public ClientVisibilityReaderBridge(IClientVisibilityGuard clientVisibilityGuard)
    {
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public Task<bool> IsClientVisibleAsync(Guid clientId, CancellationToken ct = default)
    {
        return _clientVisibilityGuard.IsVisibleAsync(clientId, ct);
    }
}
