// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for fetching the paged and filtered client list.
/// Restricts the deleted-entries view to administrators: for non-admin users
/// the ShowDeleteEntries flag is silently ignored.
/// @param request - Contains the client list filter including search, pagination and the deleted-entries flag
/// </summary>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Clients;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Clients
{
    public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedClientResource>
    {
        private readonly IClientFilterRepository _clientFilterRepository;
        private readonly IClientRepository _clientRepository;
        private readonly ClientMapper _clientMapper;
        private readonly FilterMapper _filterMapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<GetTruncatedListQueryHandler> _logger;

        public GetTruncatedListQueryHandler(
            IClientFilterRepository clientFilterRepository,
            IClientRepository clientRepository,
            ClientMapper clientMapper,
            FilterMapper filterMapper,
            IHttpContextAccessor httpContextAccessor,
            ILogger<GetTruncatedListQueryHandler> logger)
        {
            _clientFilterRepository = clientFilterRepository;
            _clientRepository = clientRepository;
            _clientMapper = clientMapper;
            _filterMapper = filterMapper;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task<TruncatedClientResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching truncated client list");

            if (request.Filter == null)
            {
                _logger.LogWarning("Filter parameter is null for truncated client list");
                throw new InvalidRequestException("Filter parameter is required for truncated client list");
            }

            try
            {
                var clientFilter = _filterMapper.ToClientFilter(request.Filter);
                RestrictDeletedEntriesToAdmins(clientFilter);
                var pagination = _filterMapper.ToPaginationParams(request.Filter);

                var pagedResult = await _clientFilterRepository.GetFilteredClients(clientFilter, pagination);
                var lastChangeMetaData = await _clientRepository.LastChangeMetaData();
                
                var truncatedClients = _clientMapper.ToTruncatedClient(pagedResult);
                truncatedClients.Editor = lastChangeMetaData.Author;
                truncatedClients.LastChange = lastChangeMetaData.LastChangesDate;
                
                _logger.LogInformation("Retrieved truncated client list with {Count} clients", pagedResult.Items.Count());
                
                return _clientMapper.ToTruncatedResource(truncatedClients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while fetching truncated client list");
                throw new InvalidRequestException($"Failed to retrieve truncated client list: {ex.Message}");
            }
        }

        private void RestrictDeletedEntriesToAdmins(ClientFilter clientFilter)
        {
            if (!clientFilter.ShowDeleteEntries)
            {
                return;
            }

            var isAdmin = _httpContextAccessor.HttpContext?.User?.IsInRole(Roles.Admin) == true;
            if (!isAdmin)
            {
                clientFilter.ShowDeleteEntries = false;
                _logger.LogInformation("ShowDeleteEntries was requested by a non-admin user and has been ignored");
            }
        }
    }
}
