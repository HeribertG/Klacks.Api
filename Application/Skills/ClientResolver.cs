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
using Klacks.Api.Application.Services.Clients;
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
        return await ClientReferenceGuard.ExistsVisibleAsync(
            clientRepository, visibilityGuard, clientId, cancellationToken);
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
            idNumber: null, idNumberSupported: false, IdNumberParameterName, cancellationToken);
    }

    /// <summary>
    /// Resolves a client by name and optional client number. The ambiguity and wrong-number answers name the
    /// parameter the skill really has for the number, so a skill that resolves a second, prefixed client (for example
    /// targetIdNumber) tells the model the right retry parameter.
    /// </summary>
    /// <param name="idNumber">Visible client number that disambiguates duplicate names</param>
    /// <param name="idNumberParameterName">Name of the skill parameter carrying the number, named in the answers</param>
    public static Task<(Client? Client, string? Error)> ResolveByNameAsync(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        string? firstName,
        string? lastName,
        int? idNumber,
        CancellationToken cancellationToken,
        string idNumberParameterName = IdNumberParameterName)
    {
        return ResolveCoreAsync(
            searchRepository, clientRepository, firstName, lastName,
            idNumber, idNumberSupported: true, idNumberParameterName, cancellationToken);
    }

    /// <summary>
    /// Resolves the client a skill call names, either by an id or by first/last name (optionally with the visible
    /// client number). An id that is unknown and an id the caller may not see give the same answer; a name is looked
    /// up through the group-scoped search, so a hidden person is unknown there too.
    /// </summary>
    /// <param name="clientIdRaw">Client id as the model supplied it; when present the names are ignored</param>
    /// <param name="firstName">First name as the caller supplied it</param>
    /// <param name="lastName">Last name as the caller supplied it</param>
    /// <param name="idNumber">Visible client number that disambiguates duplicate names</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="idNumberParameterName">Name of the skill parameter carrying the number, named in the answers</param>
    public static async Task<(Client? Client, string? Error)> ResolveByIdOrNameAsync(
        IClientSearchRepository searchRepository,
        IClientRepository clientRepository,
        IClientVisibilityGuard visibilityGuard,
        string? clientIdRaw,
        string? firstName,
        string? lastName,
        int? idNumber,
        CancellationToken cancellationToken,
        string idNumberParameterName = IdNumberParameterName)
    {
        if (string.IsNullOrWhiteSpace(clientIdRaw))
        {
            return await ResolveByNameAsync(
                searchRepository, clientRepository, firstName, lastName, idNumber, cancellationToken,
                idNumberParameterName);
        }

        if (!Guid.TryParse(clientIdRaw, out var clientId))
        {
            return (null, $"Invalid client id '{clientIdRaw}'. Expected a UUID, or give the first and last name instead.");
        }

        var client = await LoadVisibleByIdAsync(clientRepository, visibilityGuard, clientId, cancellationToken);
        return client == null ? (null, $"Client {clientId} not found.") : (client, null);
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
        string idNumberParameterName,
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
                $"Matches: {Describe(items)}. Use one of these client numbers (the number in brackets) as " +
                $"'{idNumberParameterName}'.");
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
              $"'{idNumberParameterName}' set to that client number "
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
