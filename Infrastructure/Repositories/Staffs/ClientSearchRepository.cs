// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Searches and filters clients by combinable criteria — search term, canton, entity type,
/// active contract, city, zip prefix and currently valid qualification — scoped to the
/// caller's visible groups. The canton/city/zip filters intentionally match ANY address type
/// of the client, while the returned display columns always show the employee address.
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
    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;

    public ClientSearchRepository(DataBaseContext context, IClientGroupFilterService groupFilterService)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
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
        if (limit > 100) limit = 100;

        var query = context.Client
            .Include(c => c.Addresses)
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .AsQueryable();

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

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.Name)
            .ThenBy(c => c.FirstName)
            .Take(limit)
            .Select(c => new ClientSearchItem
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
            })
            .ToListAsync(cancellationToken);

        return new ClientSearchResult
        {
            Items = items,
            TotalCount = totalCount
        };
    }
}