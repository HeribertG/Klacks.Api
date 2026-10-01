// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Group-visibility check for client-owned data, delegating to the client search repository so the rule is the
/// one ClientGroupFilterService applies to the client list (including "no calling user means unrestricted").
/// Never resolve the scope from IGroupVisibilityService directly here: without a user it reports
/// Restricted([], []), which would lock out background jobs. An unrestricted caller is answered without a
/// query, so admins and owner-less background work keep their previous behaviour on unknown or soft-deleted
/// clients (the handler's own existence check decides).
/// </summary>
/// <param name="clientSearchRepository">Answers which client ids the calling user may see</param>
/// <param name="clientGroupFilterService">Tells whether the caller is limited by group visibility at all</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Application.Services.Clients;

public class ClientVisibilityGuard : IClientVisibilityGuard
{
    private readonly IClientSearchRepository _clientSearchRepository;
    private readonly IClientGroupFilterService _clientGroupFilterService;

    public ClientVisibilityGuard(
        IClientSearchRepository clientSearchRepository, IClientGroupFilterService clientGroupFilterService)
    {
        _clientSearchRepository = clientSearchRepository;
        _clientGroupFilterService = clientGroupFilterService;
    }

    public async Task<bool> IsVisibleAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        if (await _clientGroupFilterService.IsCallerUnrestrictedAsync())
        {
            return true;
        }

        return await _clientSearchRepository.IsVisibleToCallerAsync(clientId, cancellationToken);
    }

    public async Task<bool> AreAllVisibleAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = clientIds.Distinct().ToList();
        if (distinctIds.Count == 0 || await _clientGroupFilterService.IsCallerUnrestrictedAsync())
        {
            return true;
        }

        var visibleIds = await _clientSearchRepository.FilterVisibleToCallerAsync(distinctIds, cancellationToken);
        return distinctIds.All(visibleIds.Contains);
    }

    public async Task<List<T>> FilterVisibleAsync<T>(
        IReadOnlyCollection<T> items, Func<T, Guid> clientIdOf, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
        {
            return [];
        }

        if (await _clientGroupFilterService.IsCallerUnrestrictedAsync())
        {
            return items.ToList();
        }

        var clientIds = items.Select(clientIdOf).Distinct().ToList();
        var visibleIds = await _clientSearchRepository.FilterVisibleToCallerAsync(clientIds, cancellationToken);
        return items.Where(item => visibleIds.Contains(clientIdOf(item))).ToList();
    }
}
