// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
                IdNumber = c.IdNumber
            })
            .ToListAsync();
    }

    public async Task<ClientSearchResult> SearchAsync(
        string? searchTerm = null,
        string? canton = null,
        EntityTypeEnum? entityType = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (limit > 100) limit = 100;

        var query = context.Client
            .Include(c => c.Addresses)
            .Where(c => !c.IsDeleted)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(c =>
                (c.FirstName != null && c.FirstName.ToLower().Contains(term)) ||
                (c.Name != null && c.Name.ToLower().Contains(term)) ||
                (c.Company != null && c.Company.ToLower().Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(canton))
        {
            query = query.Where(c =>
                c.Addresses.Any(a => a.State != null && a.State.ToUpper() == canton.ToUpper()));
        }

        if (entityType.HasValue)
        {
            query = query.Where(c => c.Type == entityType.Value);
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