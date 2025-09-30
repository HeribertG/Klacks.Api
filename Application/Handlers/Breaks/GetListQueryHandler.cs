using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Breaks;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Breaks;

public class GetListQueryHandler : IRequestHandler<ListQuery, IEnumerable<ClientBreakResource>>
{
    private readonly IClientRepository _clientRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(
        IClientRepository clientRepository, 
        IMapper mapper, 
        ILogger<GetListQueryHandler> logger)
    {
        _clientRepository = clientRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ClientBreakResource>> Handle(ListQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Fetching client break list with filter");
            
            if (request.Filter == null)
            {
                _logger.LogWarning("Break filter is null");
                throw new InvalidRequestException("Filter parameter is required for break list query");
            }
            
            var breakFilter = _mapper.Map<BreakFilter>(request.Filter);
            var pagination = new PaginationParams { PageIndex = 0, PageSize = 1000 }; // Default für Liste
            
            var clients = await _clientRepository.BreakList(breakFilter, pagination);
            var clientList = clients.ToList();
            
            _logger.LogInformation($"Retrieved {clientList.Count} clients with break data");
            
            return _mapper.Map<IEnumerable<ClientBreakResource>>(clientList);
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
