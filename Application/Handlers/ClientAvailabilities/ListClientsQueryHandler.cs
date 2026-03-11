// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler für die Client-Availability Client-Liste mit Suche, Gruppen- und Typ-Filter.
/// </summary>
/// <param name="request">Query mit Filter für Suche, Gruppe und Paging</param>
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class ListClientsQueryHandler : BaseHandler, IRequestHandler<ListClientAvailabilityClientsQuery, ClientAvailabilityClientListResponse>
{
    private readonly DataBaseContext _context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;

    public ListClientsQueryHandler(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService,
        ILogger<ListClientsQueryHandler> logger)
        : base(logger)
    {
        _context = context;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
    }

    public async Task<ClientAvailabilityClientListResponse> Handle(
        ListClientAvailabilityClientsQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var startOfYear = new DateTime(request.Filter.StartDate.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfYear = new DateTime(request.Filter.EndDate.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);

            var query = _context.Client
                .Include(c => c.Membership)
                .Include(c => c.GroupItems)
                .Where(c => c.Type != EntityTypeEnum.Customer)
                .Where(c => c.Membership != null &&
                           c.Membership.ValidFrom <= endOfYear &&
                           (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startOfYear))
                .AsQueryable();

            query = await _groupFilterService.FilterClientsByGroupId(request.Filter.SelectedGroup, query);
            query = _searchFilterService.ApplySearchFilter(query, request.Filter.SearchString, false);
            query = ApplyTypeFilter(query, request.Filter.ShowEmployees, request.Filter.ShowExtern);
            query = ApplySorting(query, request.Filter.OrderBy, request.Filter.SortOrder);

            var totalCount = await query.CountAsync(cancellationToken);

            var clients = await query
                .Skip(request.Filter.StartRow)
                .Take(request.Filter.RowCount)
                .ToListAsync(cancellationToken);

            return new ClientAvailabilityClientListResponse
            {
                Clients = clients.Select(MapToResource).ToList(),
                TotalCount = totalCount
            };
        }, "ListClientAvailabilityClients", new { request.Filter.SearchString });
    }

    private static ClientAvailabilityClientResource MapToResource(Client client)
    {
        return new ClientAvailabilityClientResource
        {
            Id = client.Id,
            Name = client.Name ?? string.Empty,
            FirstName = client.FirstName ?? string.Empty,
            Company = client.Company ?? string.Empty,
            LegalEntity = client.LegalEntity,
            IdNumber = client.IdNumber,
            GroupIds = client.GroupItems?.Select(g => g.GroupId).ToList() ?? []
        };
    }

    private static IQueryable<Client> ApplyTypeFilter(IQueryable<Client> query, bool showEmployees, bool showExtern)
    {
        if (!showEmployees && !showExtern)
            return query.Where(c => false);
        if (!showEmployees)
            return query.Where(c => c.Type == EntityTypeEnum.ExternEmp);
        if (!showExtern)
            return query.Where(c => c.Type == EntityTypeEnum.Employee);
        return query;
    }

    private static IQueryable<Client> ApplySorting(IQueryable<Client> query, string orderBy, string sortOrder)
    {
        var isAscending = sortOrder.Equals("asc", StringComparison.OrdinalIgnoreCase);

        return orderBy.ToLower() switch
        {
            "firstname" => isAscending
                ? query.OrderBy(c => c.FirstName).ThenBy(c => c.Name)
                : query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.Name),
            "company" => isAscending
                ? query.OrderBy(c => c.Company).ThenBy(c => c.Name)
                : query.OrderByDescending(c => c.Company).ThenByDescending(c => c.Name),
            _ => isAscending
                ? query.OrderBy(c => c.Name).ThenBy(c => c.FirstName)
                : query.OrderByDescending(c => c.Name).ThenByDescending(c => c.FirstName)
        };
    }
}
