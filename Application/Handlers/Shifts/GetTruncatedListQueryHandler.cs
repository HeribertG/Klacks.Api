using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetTruncatedListQueryHandler> _logger;

    public GetTruncatedListQueryHandler(IShiftRepository shiftRepository, IMapper mapper, ILogger<GetTruncatedListQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<TruncatedShiftResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching truncated shift list");
        
        try
        {
            var truncatedShift = await _shiftRepository.GetFilteredAndPaginatedShifts(request.Filter);
            
            _logger.LogInformation($"Retrieved truncated shift list with {truncatedShift.Shifts?.Count() ?? 0} shifts");
            
            return _mapper.Map<TruncatedShiftResource>(truncatedShift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching truncated shift list");
            throw new InvalidRequestException($"Failed to retrieve truncated shift list: {ex.Message}");
        }
    }
}