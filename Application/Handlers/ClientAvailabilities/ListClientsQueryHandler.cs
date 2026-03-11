// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler für die Client-Availability Client-Liste.
/// Nutzt den zentralen ClientBaseQueryService für einheitliche Filterung.
/// </summary>
/// <param name="request">Query mit Filter für Suche, Gruppe und Paging</param>
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ClientAvailabilities;

public class ListClientsQueryHandler : BaseHandler, IRequestHandler<ListClientAvailabilityClientsQuery, ClientAvailabilityClientListResponse>
{
    private readonly IClientBaseQueryService _baseQueryService;

    public ListClientsQueryHandler(
        IClientBaseQueryService baseQueryService,
        ILogger<ListClientsQueryHandler> logger)
        : base(logger)
    {
        _baseQueryService = baseQueryService;
    }

    public async Task<ClientAvailabilityClientListResponse> Handle(
        ListClientAvailabilityClientsQuery request,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            _logger.LogWarning("[DEBUG-CA] Filter: SearchString={Search}, Start={Start}, End={End}, ShowEmp={Emp}, ShowExt={Ext}, Group={Group}, StartRow={SR}, RowCount={RC}",
                request.Filter.SearchString, request.Filter.StartDate, request.Filter.EndDate,
                request.Filter.ShowEmployees, request.Filter.ShowExtern, request.Filter.SelectedGroup,
                request.Filter.StartRow, request.Filter.RowCount);

            var baseFilter = new ClientBaseFilter
            {
                StartDate = request.Filter.StartDate,
                EndDate = request.Filter.EndDate,
                SearchString = request.Filter.SearchString,
                SelectedGroup = request.Filter.SelectedGroup,
                ShowEmployees = request.Filter.ShowEmployees,
                ShowExtern = request.Filter.ShowExtern,
                OrderBy = request.Filter.OrderBy,
                SortOrder = request.Filter.SortOrder,
            };

            var query = await _baseQueryService.BuildBaseQuery(baseFilter);

            query = query.Include(c => c.GroupItems);

            var totalCount = await query.CountAsync(cancellationToken);

            _logger.LogWarning("[DEBUG-CA] TotalCount={Count}", totalCount);

            var clients = await query
                .Skip(request.Filter.StartRow)
                .Take(request.Filter.RowCount)
                .ToListAsync(cancellationToken);

            _logger.LogWarning("[DEBUG-CA] Returned {Count} clients: {Names}",
                clients.Count, string.Join(", ", clients.Select(c => $"{c.Name} {c.FirstName}")));

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
}
