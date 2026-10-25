// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Searches and filters clients by combinable criteria — search term, canton, entity type,
/// active contract, city, zip prefix and currently valid qualification. The canton/city/zip
/// filters intentionally match ANY address type of the client (one non-deleted address must meet all of them,
/// the shared AddressLocationPredicate), while the returned display columns always show the employee address.
/// <para>
/// Group visibility is NOT applied uniformly across this repository, so read the per-method
/// contract before adding a caller:
/// <list type="bullet">
/// <item><description><c>SearchAsync</c> (both overloads, plain text match without a fuzzy fallback),
/// <c>SearchByFuzzyNameAsync</c> (trigram and phonetic ranking, only for callers allowed by ClientFuzzySearchGuardTests),
/// <c>IsVisibleToCallerAsync</c>, <c>FilterVisibleToCallerAsync</c> and <c>GetClientsForReplacement</c> run every result through
/// <see cref="IClientGroupFilterService"/> and are therefore scoped to the caller's visible
/// groups — the total count is taken after that scoping so an invisible client never shows up
/// as a hidden remainder either.</description></item>
/// <item><description><c>FindList</c> is deliberately unscoped: it is the duplicate check when a new client
/// is created, and a duplicate hidden in a foreign group must still be found (owner decision 2026-10-01 —
/// knowing who is employed is acceptable, a duplicate is not). It returns nothing when every parameter is
/// blank, so it cannot be used to list the whole tenant in one call.</description></item>
/// <item><description><c>FindByMail</c> and <c>FindStatePostCode</c> are
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
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Geo;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Domain.Services.Geo;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Staffs;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Infrastructure.Services.Clients;

namespace Klacks.Api.Infrastructure.Repositories.Staffs;

public class ClientSearchRepository : IClientSearchRepository
{
    private const int FuzzyCandidateLimit = 50;
    private const int MaxSearchLimit = 100;

    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientFuzzySearchService _fuzzySearchService;
    private readonly ICompanyClock _companyClock;

    public ClientSearchRepository(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientFuzzySearchService fuzzySearchService,
        ICompanyClock companyClock)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
        _fuzzySearchService = fuzzySearchService;
        _companyClock = companyClock;
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
        if (string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(firstname))
        {
            return [];
        }

        IQueryable<Client> query = this.context.Client;

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

    public async Task<bool> IsVisibleToCallerAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var query = await _groupFilterService.FilterClientsByGroupId(
            null, this.context.Client.Where(client => client.Id == clientId));

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<Guid>> FilterVisibleToCallerAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken = default)
    {
        if (clientIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var distinctIds = clientIds.Distinct().ToList();
        var query = await _groupFilterService.FilterClientsByGroupId(
            null, this.context.Client.Where(client => distinctIds.Contains(client.Id)));

        var visibleIds = await query.Select(client => client.Id).ToListAsync(cancellationToken);
        return visibleIds.ToHashSet();
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
            .AsNoTracking();

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
            .AsNoTracking();

        query = await _groupFilterService.FilterClientsByGroupId(null, query);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var tokens = SearchStringTokenizer.Tokenize(searchTerm);
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

        var effectiveQualificationValidityDate = await ResolveQualificationValidityDateAsync(
            qualificationId, qualificationValidityDate, cancellationToken);
        query = ApplyStructuredFilters(
            query, canton, entityType, contractId, city, zipPrefix, qualificationId, effectiveQualificationValidityDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ProjectToItems(query
                .OrderBy(c => c.Name)
                .ThenBy(c => c.FirstName)
                .Take(limit))
            .ToListAsync(cancellationToken);

        return new ClientSearchResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<ClientSearchResult> SearchByFuzzyNameAsync(
        string searchTerm,
        string? canton = null,
        EntityTypeEnum? entityType = null,
        Guid? contractId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new ClientSearchResult { Items = [], TotalCount = 0 };
        }

        if (limit > MaxSearchLimit) limit = MaxSearchLimit;

        var items = await SearchFuzzyAsync(
            searchTerm, canton, entityType, contractId, city: null, zipPrefix: null,
            qualificationId: null, qualificationValidityDate: null, limit, cancellationToken);

        return new ClientSearchResult
        {
            Items = items,
            TotalCount = items.Count
        };
    }

    public async Task<IReadOnlyList<Guid>> ListMatchingIdsAsync(
        string? canton,
        EntityTypeEnum? entityType,
        Guid? contractId,
        string? city,
        string? zipPrefix,
        Guid? qualificationId,
        DateOnly? qualificationValidityDate,
        CancellationToken cancellationToken)
    {
        var query = context.Client
            .Where(c => !c.IsDeleted)
            .AsNoTracking();

        query = await _groupFilterService.FilterClientsByGroupId(null, query);

        var effectiveQualificationValidityDate = await ResolveQualificationValidityDateAsync(
            qualificationId, qualificationValidityDate, cancellationToken);
        query = ApplyStructuredFilters(
            query, canton, entityType, contractId, city, zipPrefix, qualificationId, effectiveQualificationValidityDate);

        return await query
            .OrderBy(c => c.Id)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ClientSearchItem>> GetItemsByIdsAsync(
        IReadOnlyCollection<Guid> clientIds, CancellationToken cancellationToken)
    {
        if (clientIds.Count == 0)
        {
            return [];
        }

        var ids = clientIds.Distinct().ToList();
        var query = context.Client
            .Include(c => c.Addresses)
            .Where(c => !c.IsDeleted && ids.Contains(c.Id))
            .AsNoTracking();

        query = await _groupFilterService.FilterClientsByGroupId(null, query);

        return await ProjectToItems(query
                .OrderBy(c => c.Name)
                .ThenBy(c => c.FirstName))
            .ToListAsync(cancellationToken);
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
            .AsNoTracking();

        // The fuzzy service ranks by name similarity across all groups; the caller-visible
        // subset is established here, exactly as the other fuzzy fallbacks do it.
        query = await _groupFilterService.FilterClientsByGroupId(null, query);

        var effectiveQualificationValidityDate = await ResolveQualificationValidityDateAsync(
            qualificationId, qualificationValidityDate, cancellationToken);
        query = ApplyStructuredFilters(
            query, canton, entityType, contractId, city, zipPrefix, qualificationId, effectiveQualificationValidityDate);

        var items = await ProjectToItems(query).ToListAsync(cancellationToken);

        return items
            .OrderBy(item => rankById[item.Id])
            .Take(limit)
            .ToList();
    }

    private async Task<DateOnly?> ResolveQualificationValidityDateAsync(
        Guid? qualificationId, DateOnly? qualificationValidityDate, CancellationToken cancellationToken)
    {
        // ApplyStructuredFilters only reads this value when qualificationId is set, so the company
        // clock (a settings read) is skipped entirely for every search that does not filter by
        // qualification - the overwhelming majority of calls.
        if (!qualificationId.HasValue)
        {
            return null;
        }

        return qualificationValidityDate ?? await _companyClock.GetTodayDateAsync(cancellationToken);
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
        var location = AddressLocationCriteria.ForStateCode(canton, city, zipPrefix);
        if (!location.IsEmpty)
        {
            query = query.Where(AddressLocationPredicate.AnyAddressOf(location));
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

        if (qualificationId.HasValue && qualificationValidityDate.HasValue)
        {
            var validityDate = qualificationValidityDate.Value;
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