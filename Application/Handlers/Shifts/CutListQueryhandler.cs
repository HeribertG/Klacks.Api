using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Shifts;

public class CutListQueryhandler : IRequestHandler<CutListQuery, IEnumerable<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<CutListQueryhandler> _logger;

    public CutListQueryhandler(IShiftRepository shiftRepository, ScheduleMapper scheduleMapper, ILogger<CutListQueryhandler> logger)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ShiftResource>> Handle(CutListQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching cut shift list for ID: {Id}", request.Id);
        
        try
        {
            var cuts = await _shiftRepository.CutList(request.Id);
            var cutsList = cuts.ToList();

            _logger.LogInformation("Retrieved {Count} cut shifts for ID: {Id}", cutsList.Count, request.Id);

            return cutsList.Select(s => _scheduleMapper.ToShiftResource(s)).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching cut shift list for ID: {Id}", request.Id);
            throw new InvalidRequestException($"Failed to retrieve cut shift list for ID {request.Id}: {ex.Message}");
        }
    }
}
