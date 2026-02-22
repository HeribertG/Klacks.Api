// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.BreakPlaceholders;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders;

public class GetScheduleListQueryHandler : IRequestHandler<GetScheduleListQuery, IEnumerable<ClientBreakPlaceholderResource>>
{
    private readonly IClientBreakPlaceholderRepository _clientBreakPlaceholderRepository;
    private readonly FilterMapper _filterMapper;
    private readonly ClientMapper _clientMapper;
    private readonly ILogger<GetScheduleListQueryHandler> _logger;

    public GetScheduleListQueryHandler(
        IClientBreakPlaceholderRepository clientBreakPlaceholderRepository,
        FilterMapper filterMapper,
        ClientMapper clientMapper,
        ILogger<GetScheduleListQueryHandler> logger)
    {
        _clientBreakPlaceholderRepository = clientBreakPlaceholderRepository;
        _filterMapper = filterMapper;
        _clientMapper = clientMapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ClientBreakPlaceholderResource>> Handle(GetScheduleListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Filter == null)
            {
                throw new InvalidRequestException("Filter parameter is required for schedule list query");
            }

            var breakFilter = _filterMapper.ToBreakFilter(request.Filter);

            var (clients, _) = await _clientBreakPlaceholderRepository.BreakList(breakFilter);

            return clients.Select(c => _clientMapper.ToBreakPlaceholderResource(c)).ToList();
        }
        catch (InvalidRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching schedule break placeholder list");
            throw new InvalidRequestException($"Failed to retrieve schedule break placeholder list: {ex.Message}");
        }
    }
}
