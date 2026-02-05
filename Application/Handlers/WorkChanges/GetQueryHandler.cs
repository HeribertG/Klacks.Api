using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.WorkChanges;

public class GetQueryHandler : IRequestHandler<GetQuery<WorkChangeResource>, WorkChangeResource>
{
    private readonly IWorkChangeRepository _workChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(
        IWorkChangeRepository workChangeRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetQueryHandler> logger)
    {
        _workChangeRepository = workChangeRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<WorkChangeResource> Handle(GetQuery<WorkChangeResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting WorkChange with ID: {Id}", request.Id);

            var workChange = await _workChangeRepository.Get(request.Id);

            if (workChange == null)
            {
                throw new KeyNotFoundException($"WorkChange with ID {request.Id} not found");
            }

            var result = _scheduleMapper.ToWorkChangeResource(workChange);
            _logger.LogInformation("Successfully retrieved WorkChange with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving WorkChange with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving WorkChange with ID {request.Id}: {ex.Message}");
        }
    }
}
