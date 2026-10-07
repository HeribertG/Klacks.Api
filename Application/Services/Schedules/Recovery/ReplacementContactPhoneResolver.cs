// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves contact phone numbers for replacement candidates: narrows the ids to the caller's visible employees,
/// reads their phone entries with one query and picks one number each via <see cref="PreferredPhoneSelector"/>.
/// </summary>
/// <param name="communicationRepository">Reads the phone entries</param>
/// <param name="clientVisibilityGuard">Drops employees the caller may not see</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public sealed class ReplacementContactPhoneResolver : IReplacementContactPhoneResolver
{
    private readonly ICommunicationRepository _communicationRepository;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public ReplacementContactPhoneResolver(
        ICommunicationRepository communicationRepository,
        IClientVisibilityGuard clientVisibilityGuard)
    {
        _communicationRepository = communicationRepository;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<IReadOnlyDictionary<Guid, string>> ResolveAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default)
    {
        var distinctIds = clientIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (distinctIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var visibleIds = await _clientVisibilityGuard.FilterVisibleAsync(distinctIds, id => id, cancellationToken);
        if (visibleIds.Count == 0)
        {
            return new Dictionary<Guid, string>();
        }

        var entries = await _communicationRepository.GetPhoneEntriesAsync(visibleIds, cancellationToken);
        var visible = visibleIds.ToHashSet();

        return entries
            .Where(entry => visible.Contains(entry.ClientId))
            .GroupBy(entry => entry.ClientId)
            .Select(group => (group.Key, Phone: PreferredPhoneSelector.Select(group)))
            .Where(pair => pair.Phone is not null)
            .ToDictionary(pair => pair.Key, pair => pair.Phone!);
    }
}
