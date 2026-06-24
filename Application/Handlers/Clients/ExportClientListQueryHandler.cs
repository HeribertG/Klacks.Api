// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that returns a flat list of clients for CSV export on the frontend.
/// Applies the active filter and respects the selection (include / inverted-exclude) logic.
/// </summary>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Clients;

public class ExportClientListQueryHandler : IRequestHandler<ExportClientListQuery, List<ExportClientItemDto>>
{
    private readonly IClientFilterRepository _clientFilterRepository;
    private readonly FilterMapper _filterMapper;
    private readonly ILogger<ExportClientListQueryHandler> _logger;

    public ExportClientListQueryHandler(
        IClientFilterRepository clientFilterRepository,
        FilterMapper filterMapper,
        ILogger<ExportClientListQueryHandler> logger)
    {
        _clientFilterRepository = clientFilterRepository;
        _filterMapper = filterMapper;
        _logger = logger;
    }

    public async Task<List<ExportClientItemDto>> Handle(ExportClientListQuery request, CancellationToken cancellationToken)
    {
        if (request.Request?.Filter == null)
            throw new InvalidRequestException("Filter is required for client export");

        var clientFilter = _filterMapper.ToClientFilter(request.Request.Filter);
        var query = await _clientFilterRepository.FilterClients(clientFilter);

        var selection = request.Request.Selection ?? [];

        if (!request.Request.SelectAll && !request.Request.InvertedSelection && selection.Count > 0)
            query = query.Where(c => selection.Contains(c.Id));
        else if (request.Request.InvertedSelection && selection.Count > 0)
            query = query.Where(c => !selection.Contains(c.Id));

        var clients = await query
            .OrderBy(c => c.IdNumber)
            .Select(c => new ExportClientItemDto
            {
                IdNumber = c.IdNumber,
                Company = c.Company ?? string.Empty,
                FirstName = c.FirstName ?? string.Empty,
                Name = c.Name ?? string.Empty,
                Birthdate = c.Birthdate,
                Gender = (int)c.Gender,
                Type = (int)c.Type,
                LegalEntity = c.LegalEntity,
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Exported {Count} clients", clients.Count);
        return clients;
    }
}
