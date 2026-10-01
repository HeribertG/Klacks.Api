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

    Task<ClientSearchResult> SearchAsync(
        string? searchTerm = null,
        string? canton = null,
        EntityTypeEnum? entityType = null,
        Guid? contractId = null,
        int limit = 10,
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
}

public record ClientSearchResult
{
    public required IReadOnlyList<ClientSearchItem> Items { get; init; }
    public int TotalCount { get; init; }
}

public record ClientSearchItem
{
    public Guid Id { get; init; }
    public int? IdNumber { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Company { get; init; }
    public string? Gender { get; init; }
    public string? EntityType { get; init; }
    public string? Canton { get; init; }
    public string? City { get; init; }
}