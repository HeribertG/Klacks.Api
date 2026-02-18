using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetTruncatedListQueryHandler : IRequestHandler<GetTruncatedListQuery, TruncatedShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetTruncatedListQueryHandler> _logger;

    public GetTruncatedListQueryHandler(IShiftRepository shiftRepository, ScheduleMapper scheduleMapper, ILogger<GetTruncatedListQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<TruncatedShiftResource> Handle(GetTruncatedListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching truncated shift list");
        
        try
        {
            var truncatedShift = await _shiftRepository.GetFilteredAndPaginatedShifts(request.Filter);
            
            _logger.LogInformation("Retrieved truncated shift list with {Count} shifts", truncatedShift.Shifts?.Count() ?? 0);
            
            return _scheduleMapper.ToTruncatedShiftResource(truncatedShift);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching truncated shift list");
            throw new InvalidRequestException($"Failed to retrieve truncated shift list: {ex.Message}");
        }
    }
}