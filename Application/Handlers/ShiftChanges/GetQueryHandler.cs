using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ShiftChanges;

public class GetQueryHandler : IRequestHandler<GetQuery<ShiftChangeResource>, ShiftChangeResource>
{
    private readonly IShiftChangeRepository _shiftChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(
        IShiftChangeRepository shiftChangeRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
    {
        _shiftChangeRepository = shiftChangeRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<ShiftChangeResource> Handle(GetQuery<ShiftChangeResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting ShiftChange with ID: {Id}", request.Id);

            var shiftChange = await _shiftChangeRepository.Get(request.Id);

            if (shiftChange == null)
            {
                throw new KeyNotFoundException($"ShiftChange with ID {request.Id} not found");
            }

            var result = _scheduleMapper.ToShiftChangeResource(shiftChange);
            _logger.LogInformation("Successfully retrieved ShiftChange with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ShiftChange with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving ShiftChange with ID {request.Id}: {ex.Message}");
        }
    }
}
