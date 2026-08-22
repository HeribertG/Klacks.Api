// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Searches and filters clients by combinable criteria — search term, canton, entity type,
/// active contract, city, zip prefix and currently valid qualification. The canton/city/zip
/// filters intentionally match ANY address type of the client, while the returned display
/// columns always show the employee address.
/// <para>
/// Group visibility is NOT applied uniformly across this repository, so read the per-method
/// contract before adding a caller:
/// <list type="bullet">
/// <item><description><c>SearchAsync</c> (both overloads, including its fuzzy fallback) and
/// <c>GetClientsForReplacement</c> run every result through
/// <see cref="IClientGroupFilterService"/> and are therefore scoped to the caller's visible
/// groups — the total count is taken after that scoping so an invisible client never shows up
/// as a hidden remainder either.</description></item>
/// <item><description><c>FindByMail</c>, <c>FindList</c> and <c>FindStatePostCode</c> are
/// deliberately unscoped identity/synchronisation lookups: they run in contexts that have no
/// calling user whose visibility could apply, and must not be used to answer user-facing
/// search requests.</description></item>
/// </list>
/// </para>
/// </summary>
/// <param name="searchTerm">Optional free-text term matched against client names and mail.</param>
/// <param name="qualificationValidityDate">Reference date a qualification must be valid on.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Staffs;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientSearchRepository : IClientSearchRepository
{
    private const int FuzzyCandidateLimit = 50;
    private const int MaxSearchLimit = 100;

    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientFuzzySearchService _fuzzySearchService;

    public ClientSearchRepository(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientFuzzySearchService fuzzySearchService)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
        _fuzzySearchService = fuzzySearchService;
    }

    public async Task<Client?> FindByMail(string mail)
    {
        var query = await this.context.Communication.FirstOrDefaultAsync(x => x.Value == mail && (x.Type == CommunicationTypeEnum.OfficeMail || x.Type == CommunicationTypeEnum.PrivateMail));
        if (query != null)
        {
            return await this.context.Client.FirstOrDefaultAsync(x => x.Id == query.ClientId);
        }

        return null!;
    }

    public async Task<List<Client>> FindList(string? company = null, string? name = null, string? firstname = null)
    {
        var query = this.context.Client.AsQueryable();

        if (!string.IsNullOrWhiteSpace(company))
        {
            query = query.Where(x => x.Company!.ToLower().Contains(company.ToLower().Trim()));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(x => x.Name!.ToLower().Contains(name.ToLower().Trim()));
        }

        if (!string.IsNullOrWhiteSpace(firstname))
        {
            query = query.Where(x => x.FirstName!.ToLower().Contains(firstname.ToLower().Trim()));
        }

        return await query.ToListAsync();
    }

    public async Task<string> FindStatePostCode(string zip)
    {
        int zipCode;
        if (int.TryParse(zip, out zipCode))
        {
            var result = await this.context.PostcodeCH.FirstOrDefaultAsync(x => x.Zip == zipCode);
            if (result != null)
            {
                return result.State;
            }
        }

        return string.Empty;
    }

    public async Task<List<ClientForReplacementResource>> GetClientsForReplacement()
    {
        var query = this.context.Client
            .Include(c => c.GroupItems)
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .AsNoTracking()
            .AsQueryable();

        query = await _groupFilterService.FilterClientsByGroupId(null, query);

        return await query
            .OrderBy(c => c.IdNumber)
            .Select(c => new ClientForReplacementResource
            {
                Id = c.Id,
                Name = c.Name,
                FirstName = c.FirstName,
                Company = c.Company,
                LegalEntity = c.LegalEntity,
                IdNumber = c.IdNumber,
                GroupIds = c.GroupItems.Select(g => g.GroupId).ToList()
            })
            .ToListAsync();
    }

    public Task<ClientSearchResult> SearchAsync(
        string? searchTerm = null,
        string? canton = null,
        EntityTypeEnum? entityType = null,
        Guid? contractId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return SearchAsync(
            searchTerm, canton, entityType, contractId,
            city: null, zipPrefix: null, qualificationId: null, qualificationValidityDate: null,
            limit: limit, cancellationToken: cancellationToken);
    }

    public async Task<ClientSearchResult> SearchAsync(
        string? searchTerm,
        string? canton,
        EntityTypeEnum? entityType,
        Guid? contractId,
        string? city,
        string? zipPrefix,
        Guid? qualificationId,
        DateOnly? qualificationValidityDate,
        int limit,
        CancellationToken cancellationToken)
    {
        if (limit > MaxSearchLimit) limit = MaxSearchLimit;

        var query = context.Client
            .Include(c => c.Addresses)
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .AsQueryable();

        query = await _groupFilterService.FilterClientsByGroupId(null, query);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var tokens = searchTerm.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawToken in tokens)
            {
                var token = rawToken;
                var isIdNumberToken = int.TryParse(token, out var idNumberToken);
                query = query.Where(c =>
                    (c.FirstName != null && c.FirstName.ToLower().Contains(token)) ||
                    (c.Name != null && c.Name.ToLower().Contains(token)) ||
                    (c.Company != null && c.Company.ToLower().Contains(token)) ||
                    (isIdNumberToken && c.IdNumber == idNumberToken));
            }
        }

        query = ApplyStructuredFilters(
            query, canton, entityType, contractId, city, zipPrefix, qualificationId, qualificationValidityDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectToItems(query
                .OrderBy(c => c.Name)
                .ThenBy(c => c.FirstName)
                .Take(limit))
            .ToListAsync(cancellationToken);

        // A spoken or misheard name never survives the substring token filter — rank the term
        // fuzzily (trigram + phonetics) and keep only candidates the caller may see and the
        // structured filters permit.
        if (items.Count == 0 && !string.IsNullOrWhiteSpace(searchTerm))
        {
            var fuzzyItems = await SearchFuzzyAsync(
                searchTerm, canton, entityType, contractId, city, zipPrefix,
                qualificationId, qualificationValidityDate, limit, cancellationToken);
            if (fuzzyItems.Count > 0)
            {
                return new ClientSearchResult
                {
                    Items = fuzzyItems,
                    TotalCount = fuzzyItems.Count
                };
            }
        }

        return new ClientSearchResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    private async Task<List<ClientSearchItem>> SearchFuzzyAsync(
        string searchTerm,
        string? canton,
        EntityTypeEnum? entityType,
        Guid? contractId,
        string? city,
        string? zipPrefix,
        Guid? qualificationId,
        DateOnly? qualificationValidityDate,
        int limit,
        CancellationToken cancellationToken)
    {
        var ranked = await _fuzzySearchService.SearchAsync(searchTerm, FuzzyCandidateLimit, cancellationToken) ?? [];
        if (ranked.Count == 0)
        {
            return [];
        }

        var rankedIds = ranked.Select(c => c.Id).ToList();
        var rankById = rankedIds
            .Select((id, index) => (Id: id, Index: index))
            .ToDictionary(x => x.Id, x => x.Index);

        var query = context.Client
            .Include(c => c.Addresses)
            .Where(c => !c.IsDeleted && rankedIds.Contains(c.Id))
            .AsNoTracking()
            .AsQueryable();

        // The fuzzy service ranks by name similarity across all groups; the caller-visible
        // subset is established here, exactly as the other fuzzy fallbacks do it.
        query = await _groupFilterService.FilterClientsByGroupId(null, query);

        query = ApplyStructuredFilters(
            query, canton, entityType, contractId, city, zipPrefix, qualificationId, qualificationValidityDate);

        var items = await ProjectToItems(query).ToListAsync(cancellationToken);

        return items
            .OrderBy(item => rankById[item.Id])
            .Take(limit)
            .ToList();
    }

    private static IQueryable<Client> ApplyStructuredFilters(
        IQueryable<Client> query,
        string? canton,
        EntityTypeEnum? entityType,
        Guid? contractId,
        string? city,
        string? zipPrefix,
        Guid? qualificationId,
        DateOnly? qualificationValidityDate)
    {
        if (!string.IsNullOrWhiteSpace(canton))
        {
            query = query.Where(c =>
                c.Addresses.Any(a => a.State != null && a.State.ToUpper() == canton.ToUpper()));
        }

        if (!string.IsNullOrWhiteSpace(city))
        {
            var trimmedCity = city.Trim();
            query = query.Where(c =>
                c.Addresses.Any(a => a.City != null && a.City.ToUpper() == trimmedCity.ToUpper()));
        }

        if (!string.IsNullOrWhiteSpace(zipPrefix))
        {
            var trimmedZipPrefix = zipPrefix.Trim().ToLower();
            query = query.Where(c =>
                c.Addresses.Any(a => a.Zip != null && a.Zip.ToLower().StartsWith(trimmedZipPrefix)));
        }

        if (entityType.HasValue)
        {
            query = query.Where(c => c.Type == entityType.Value);
        }

        if (contractId.HasValue)
        {
            query = query.Where(c =>
                c.ClientContracts.Any(cc => !cc.IsDeleted && cc.IsActive && cc.ContractId == contractId.Value));
        }

        if (qualificationId.HasValue)
        {
            var validityDate = qualificationValidityDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            query = query.Where(c =>
                c.Qualifications.Any(q =>
                    !q.IsDeleted &&
                    q.QualificationId == qualificationId.Value &&
                    (q.ValidFrom == null || q.ValidFrom <= validityDate) &&
                    (q.ValidUntil == null || q.ValidUntil >= validityDate)));
        }

        return query;
    }

    private static IQueryable<ClientSearchItem> ProjectToItems(IQueryable<Client> query)
    {
        return query.Select(c => new ClientSearchItem
        {
            Id = c.Id,
            IdNumber = c.IdNumber,
            FirstName = c.FirstName,
            LastName = c.Name,
            Company = c.Company,
            Gender = c.Gender.ToString(),
            EntityType = c.Type.ToString(),
            Canton = c.Addresses
                .Where(a => a.Type == AddressTypeEnum.Employee)
                .Select(a => a.State)
                .FirstOrDefault(),
            City = c.Addresses
                .Where(a => a.Type == AddressTypeEnum.Employee)
                .Select(a => a.City)
                .FirstOrDefault()
        });
    }
}