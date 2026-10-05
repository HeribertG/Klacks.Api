// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Shared helper for the small client-edit skills: resolves a client by first/last name
/// (via the search repository) and loads the full tracked-capable entity for an update.
/// When several clients share the name, an exact (accent-insensitive) full-name match is
/// preferred over looser search hits; genuine duplicates are disambiguated via the optional
/// idNumber parameter (the visible client number), which the ambiguity error instructs the
/// model to pass on the retry — duplicates are never resolved by silently picking one.
/// Lookups by an id the model supplied go through the client visibility guard first: a client the
/// caller may not see is answered exactly like an unknown id and is never loaded.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Skills;

internal static class ClientResolver
{
    public const string IdNumberParameterName = "idNumber";

    private const int SearchResultLimit = 10;

    /// <summary>
    /// Loads a client by an id the model supplied, or returns null when the client does not exist or
    /// is hidden from the caller by group visibility - both cases must produce the same answer.
    /// </summary>
    /// <param name="clientId">Client id taken from the skill parameters</param>
    public static async Task<Client?> LoadVisibleByIdAsync(
        IClientRepository clientRepository,
        IClientVisibilityGuard visibilityGuard,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        if (!await visibilityGuard.IsVisibleAsync(clientId, cancellationToken))
        {
            return null;
        }

        return await clientRepository.Get(clientId);
    }

    /// <summary>
    /// True only when the client exists and is visible to the caller; a hidden client reads as missing.
    /// </summary>
    /// <param name="clientId">Client id taken from the skill parameters</param>
    public static async Task<bool> ExistsVisibleAsync(
        IClientRepository clientRepository,
        IClientVisibilityGuard visibilityGuard,
        Guid clientId,
        CancellationToken cancellationToken)
    {
        return await visibilityGuard.IsVisibleAsync(clientId, cancellationToken)
               && await clientRepository.Exists(clientId);
    }

    public static Task<(Client? Client, string? Error)> ResolveByNameAsync(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        string? firstName,
        string? lastName,
        CancellationToken cancellationToken)
    {
        return ResolveCoreAsync(
            searchRepository, clientRepository, firstName, lastName,
            idNumber: null, idNumberSupported: false, cancellationToken);
    }

    public static Task<(Client? Client, string? Error)> ResolveByNameAsync(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        string? firstName,
        string? lastName,
        int? idNumber,
        CancellationToken cancellationToken)
    {
        return ResolveCoreAsync(
            searchRepository, clientRepository, firstName, lastName,
            idNumber, idNumberSupported: true, cancellationToken);
    }

    /// <summary>
    /// The answer for a name that matches nobody. A skill that finds a client the caller may not see must give
    /// exactly this answer too, so a hidden person cannot be told apart from an unknown one.
    /// </summary>
    /// <param name="firstName">First name as the caller supplied it</param>
    /// <param name="lastName">Last name as the caller supplied it</param>
    public static string NotFoundMessage(string? firstName, string? lastName) =>
        $"No client found matching '{$"{firstName} {lastName}".Trim()}'. Check the spelling with the user — " +
        "do not call this skill again with the same name.";
    private static async Task<(Client? Client, string? Error)> ResolveCoreAsync(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        string? firstName,
        string? lastName,
        int? idNumber,
        bool idNumberSupported,
        CancellationToken cancellationToken)
    {
        var term = $"{firstName} {lastName}".Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            return (null, "Please provide the client's first and last name.");
        }

        var search = await searchRepository.SearchAsync(
            term, null, null, null, SearchResultLimit, cancellationToken);
        if (search.Items.Count == 0)
        {
            return (null, NotFoundMessage(firstName, lastName));
        }

        var items = search.Items;

        if (idNumber.HasValue)
        {
            var byNumber = items.Where(i => i.IdNumber == idNumber.Value).ToList();
            if (byNumber.Count == 1)
            {
                return await LoadAsync(clientRepository, byNumber[0].Id, term);
            }

            return (null,
                $"No unique client matching '{term}' has client number {idNumber.Value}. " +
                $"Matches: {Describe(items)}. Use one of these client numbers (the number in brackets).");
        }

        if (items.Count == 1)
        {
            return await LoadAsync(clientRepository, items[0].Id, term);
        }

        var exact = ExactNameMatches(items, firstName, lastName);
        if (exact.Count == 1)
        {
            return await LoadAsync(clientRepository, exact[0].Id, term);
        }

        var pool = exact.Count > 1 ? exact : items;
        var nextStep = idNumberSupported
            ? "then call this skill again with the same name plus the additional parameter " +
              $"'{IdNumberParameterName}' set to that client number "
            : "then complete the requested action for that specific person yourself ";
        return (null,
            $"Multiple clients match '{term}': {Describe(pool)}. Ask the user which one they mean " +
            $"(the number in brackets is the client number), {nextStep}" +
            "— do not open the edit page or ask the user to do it by hand.");
    }

    private static List<ClientSearchItem> ExactNameMatches(
        IReadOnlyList<ClientSearchItem> items, string? firstName, string? lastName)
    {
        var normalizedFirst = NameMatching.Normalize(firstName);
        var normalizedLast = NameMatching.Normalize(lastName);
        if (normalizedFirst.Length == 0 && normalizedLast.Length == 0)
        {
            return new List<ClientSearchItem>();
        }

        return items
            .Where(i => (normalizedLast.Length == 0 || NameMatching.Normalize(i.LastName) == normalizedLast)
                        && (normalizedFirst.Length == 0 || NameMatching.Normalize(i.FirstName) == normalizedFirst))
            .ToList();
    }

    private static string Describe(IReadOnlyList<ClientSearchItem> items)
    {
        return string.Join(", ", items.Select(i => $"{i.FirstName} {i.LastName} (#{i.IdNumber})"));
    }

    private static async Task<(Client? Client, string? Error)> LoadAsync(
        IClientRepository clientRepository, Guid id, string term)
    {
        var client = await clientRepository.Get(id);
        return client == null
            ? (null, $"Client '{term}' could not be loaded.")
            : (client, null);
    }
}
