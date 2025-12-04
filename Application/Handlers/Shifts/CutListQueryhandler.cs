using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Shifts;

public class CutListQueryhandler : IRequestHandler<CutListQuery, IEnumerable<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CutListQueryhandler> _logger;

    public CutListQueryhandler(IShiftRepository shiftRepository, IMapper mapper, ILogger<CutListQueryhandler> logger)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ShiftResource>> Handle(CutListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Fetching cut shift list for ID: {request.Id}");
        
        try
        {
            var cuts = await _shiftRepository.CutList(request.Id);
            var cutsList = cuts.ToList();
            
            _logger.LogInformation($"Retrieved {cutsList.Count} cut shifts for ID: {request.Id}");
            
            return _mapper.Map<IEnumerable<ShiftResource>>(cutsList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Unexpected error while fetching cut shift list for ID: {request.Id}");
            throw new InvalidRequestException($"Failed to retrieve cut shift list for ID {request.Id}: {ex.Message}");
        }
    }
}
