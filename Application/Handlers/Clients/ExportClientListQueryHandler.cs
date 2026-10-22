// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler that returns a flat list of clients for CSV export on the frontend.
/// Applies the active filter and respects the selection (include / inverted-exclude) logic.
/// Soft-deleted clients are exported only for administrators; for anyone else the ShowDeleteEntries
/// flag is ignored, exactly as in the client list.
/// </summary>

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Application.Services.Clients;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Clients;

public class ExportClientListQueryHandler : IRequestHandler<ExportClientListQuery, List<ExportClientItemDto>>
{
    private readonly IClientFilterRepository _clientFilterRepository;
    private readonly FilterMapper _filterMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ExportClientListQueryHandler> _logger;

    public ExportClientListQueryHandler(
        IClientFilterRepository clientFilterRepository,
        FilterMapper filterMapper,
        IHttpContextAccessor httpContextAccessor,
        ILogger<ExportClientListQueryHandler> logger)
    {
        _clientFilterRepository = clientFilterRepository;
        _filterMapper = filterMapper;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<List<ExportClientItemDto>> Handle(ExportClientListQuery request, CancellationToken cancellationToken)
    {
        if (request.Request?.Filter == null)
            throw new InvalidRequestException("Filter is required for client export");

        var clientFilter = _filterMapper.ToClientFilter(request.Request.Filter);
        if (DeletedClientEntriesPolicy.RestrictToAdmins(clientFilter, _httpContextAccessor.HttpContext?.User))
        {
            _logger.LogInformation("ShowDeleteEntries was requested for an export by a non-admin user and has been ignored");
        }

        var selection = request.Request.Selection ?? [];
        List<Guid>? onlyIds = null;
        List<Guid>? excludedIds = null;

        if (!request.Request.SelectAll && !request.Request.InvertedSelection && selection.Count > 0)
            onlyIds = selection;
        else if (request.Request.InvertedSelection && selection.Count > 0)
            excludedIds = selection;

        var rows = await _clientFilterRepository.GetExportRowsAsync(clientFilter, onlyIds, excludedIds, cancellationToken);

        var clients = rows.Select(r => new ExportClientItemDto
        {
            IdNumber = r.IdNumber,
            Company = r.Company,
            FirstName = r.FirstName,
            Name = r.Name,
            Birthdate = r.Birthdate,
            Gender = (int)r.Gender,
            Type = (int)r.Type,
            LegalEntity = r.LegalEntity,
        }).ToList();

        _logger.LogInformation("Exported {Count} clients", clients.Count);
        return clients;
    }
}
