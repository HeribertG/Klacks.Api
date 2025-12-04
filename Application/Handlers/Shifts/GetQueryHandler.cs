using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Shifts;

public class GetQueryHandler : IRequestHandler<GetQuery<ShiftResource>, ShiftResource>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IShiftRepository shiftRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<ShiftResource> Handle(GetQuery<ShiftResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting shift with ID: {Id}", request.Id);
            
            var shift = await _shiftRepository.Get(request.Id);
            
            if (shift == null)
            {
                throw new KeyNotFoundException($"Shift with ID {request.Id} not found");
            }
            
            var result = _scheduleMapper.ToShiftResource(shift);
            _logger.LogInformation("Successfully retrieved shift with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shift with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving shift with ID {request.Id}: {ex.Message}");
        }
    }
}
