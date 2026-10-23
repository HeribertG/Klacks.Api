// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Application.DTOs.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientSearchRepository
{
    Task<Client?> FindByMail(string mail);

    Task<List<Client>> FindList(string? company = null, string? name = null, string? firstname = null);

    /// <summary>
    /// Tells whether the calling user may see the client under the group visibility rules of the client
    /// list. A client that does not exist is reported as not visible, so callers cannot tell the two apart.
    /// </summary>
    /// <param name="clientId">Id of the client to check</param>
    Task<bool> IsVisibleToCallerAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the subset of the given client ids the calling user may see, in a single query.
    /// </summary>
    /// <param name="clientIds">Ids of the clients to check; unknown ids are never part of the result</param>
    Task<IReadOnlySet<Guid>> FilterVisibleToCallerAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default);

    Task<string> FindStatePostCode(string zip);

    Task<List<ClientForReplacementResource>> GetClientsForReplacement();

    /// <summary>
    /// Text search over first name, last name, company and client number: every word of the term must be contained
    /// in one of them. There is no fuzzy fallback, so a misheard name finds nothing here.
    /// </summary>
    /// <param name="searchTerm">Optional words that must each be contained in a name, the company or the number</param>
    Task<ClientSearchResult> SearchAsync(
        string? searchTerm = null,
        string? canton = null,
        EntityTypeEnum? entityType = null,
        Guid? contractId = null,
        int limit = Domain.Constants.PagingDefaults.DefaultSearchResultLimit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ranks the term by name similarity (trigram and phonetics) for a spoken or misheard name that the text search
    /// did not find, scoped to the caller's visible groups and the structured filters. Its rows are similar names,
    /// never proof that the user named that person: a caller that writes must treat them as suggestions the user
    /// has to confirm (ClientResolver under NameMatchPolicy.Strict), and ClientFuzzySearchGuardTests restricts the
    /// callers to read-only skills and an allowlist.
    /// </summary>
    /// <param name="searchTerm">The name as the user said it</param>
    Task<ClientSearchResult> SearchByFuzzyNameAsync(
        string searchTerm,
        string? canton = null,
        EntityTypeEnum? entityType = null,
        Guid? contractId = null,
        int limit = Domain.Constants.PagingDefaults.DefaultSearchResultLimit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Extended overload adding city, zip-prefix and qualification filters. Kept as a separate,
    /// fully-required overload (no default values) so the existing short-form call sites keep
    /// resolving to the overload above without any changes.
    /// </summary>
    /// <param name="city">Optional exact, case-insensitive match against any of the client's addresses' city.</param>
    /// <param name="zipPrefix">Optional prefix match against any of the client's addresses' zip code.</param>
    /// <param name="qualificationId">Optional id of a qualification the client must currently hold.</param>
    /// <param name="qualificationValidityDate">Reference date used to decide whether the qualification is currently valid.</param>
    Task<ClientSearchResult> SearchAsync(
        string? searchTerm,
        string? canton,
        EntityTypeEnum? entityType,
        Guid? contractId,
        string? city,
        string? zipPrefix,
        Guid? qualificationId,
        DateOnly? qualificationValidityDate,
        int limit,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns the ids of every client the caller may see that matches the structured filters, uncapped and in a
    /// stable order (by id), so a seeded random sample drawn from it is reproducible between a preview and its apply.
    /// </summary>
    /// <param name="qualificationValidityDate">Reference date for the qualification filter; null means company today.</param>
    Task<IReadOnlyList<Guid>> ListMatchingIdsAsync(
        string? canton,
        EntityTypeEnum? entityType,
        Guid? contractId,
        string? city,
        string? zipPrefix,
        Guid? qualificationId,
        DateOnly? qualificationValidityDate,
        CancellationToken cancellationToken);

    /// <summary>
    /// Loads the search items for the given client ids, restricted to the clients the caller may see, ordered by name.
    /// </summary>
    /// <param name="clientIds">Ids to load; unknown or invisible ids are left out</param>
    Task<IReadOnlyList<ClientSearchItem>> GetItemsByIdsAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken);
}