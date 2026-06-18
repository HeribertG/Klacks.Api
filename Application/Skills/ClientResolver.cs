// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared helper for the small client-edit skills: resolves a client by first/last name
/// (via the search repository) and loads the full tracked-capable entity for an update.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Skills;

internal static class ClientResolver
{
    public static async Task<(Client? Client, string? Error)> ResolveByNameAsync(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken)
    {
        var term = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return (null, "Please provide the client's first and last name.");
        }

        var search = await searchRepository.SearchAsync(term, null, null, null, 5, cancellationToken);
        if (search.Items.Count == 0)
        {
            return (null, $"No client found matching '{term}'.");
        }

        if (search.Items.Count > 1)
        {
            var names = string.Join(", ", search.Items.Select(i => $"{i.FirstName} {i.LastName} (#{i.IdNumber})"));
            return (null, $"Multiple clients match '{term}': {names}. Please be more specific.");
        }

        var client = await clientRepository.Get(search.Items[0].Id);
        return client == null
            ? (null, $"Client '{term}' could not be loaded.")
            : (client, null);
    }
}
