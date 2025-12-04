using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Works;

public class GetQueryHandler : IRequestHandler<GetQuery<WorkResource>, WorkResource>
{
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IWorkRepository workRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
    {
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        this._logger = logger;
    }

    public async Task<WorkResource> Handle(GetQuery<WorkResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting work with ID: {Id}", request.Id);
            
            var work = await _workRepository.Get(request.Id);
            
            if (work == null)
            {
                throw new KeyNotFoundException($"Work with ID {request.Id} not found");
            }
            
            var result = _scheduleMapper.ToWorkResource(work);
            _logger.LogInformation("Successfully retrieved work with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving work with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving work with ID {request.Id}: {ex.Message}");
        }
    }
}
