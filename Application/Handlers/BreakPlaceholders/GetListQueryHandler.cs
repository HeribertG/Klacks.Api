using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.BreakPlaceholders;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders;

public class GetListQueryHandler : IRequestHandler<ListQuery, (IEnumerable<ClientBreakPlaceholderResource> Clients, int TotalCount)>
{
    private readonly IClientBreakPlaceholderRepository _clientBreakPlaceholderRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly FilterMapper _filterMapper;
    private readonly ClientMapper _clientMapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(
        IClientBreakPlaceholderRepository clientBreakPlaceholderRepository,
        ScheduleMapper scheduleMapper,
        FilterMapper filterMapper,
        ClientMapper clientMapper,
        ILogger<GetListQueryHandler> logger)
    {
        _clientBreakPlaceholderRepository = clientBreakPlaceholderRepository;
        _scheduleMapper = scheduleMapper;
        _filterMapper = filterMapper;
        _clientMapper = clientMapper;
        _logger = logger;
    }

    public async Task<(IEnumerable<ClientBreakPlaceholderResource> Clients, int TotalCount)> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching client break list with filter");

            if (request.Filter == null)
            {
                _logger.LogWarning("Break filter is null");
                throw new InvalidRequestException("Filter parameter is required for break list query");
            }

            var breakFilter = _filterMapper.ToBreakFilter(request.Filter);

            var (clients, totalCount) = await _clientBreakPlaceholderRepository.BreakList(breakFilter);
            var clientList = clients.ToList();

            _logger.LogInformation($"Retrieved {clientList.Count} clients with break data (Total: {totalCount})");

            var mappedClients = clientList.Select(c => _clientMapper.ToBreakPlaceholderResource(c)).ToList();
            return (mappedClients, totalCount);
        }
        catch (InvalidRequestException)
        {
            throw;
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Invalid format in break filter parameters");
            throw new InvalidRequestException("Invalid format in filter parameters");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching client break list");
            throw new InvalidRequestException($"Failed to retrieve client break list: {ex.Message}");
        }
    }
}
