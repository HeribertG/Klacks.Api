using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.BreakPlaceholders
{
    public class GetQueryHandler : IRequestHandler<GetQuery<BreakResource>, BreakResource>
    {
        private readonly IBreakPlaceholderRepository _breakPlaceholderRepository;
        private readonly ScheduleMapper _scheduleMapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IBreakPlaceholderRepository breakPlaceholderRepository, ScheduleMapper scheduleMapper, ILogger<GetQueryHandler> logger)
        {
            _breakPlaceholderRepository = breakPlaceholderRepository;
            _scheduleMapper = scheduleMapper;
            _logger = logger;
        }

        public async Task<BreakResource> Handle(GetQuery<BreakResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting break with ID: {Id}", request.Id);

                var breakEntity = await _breakPlaceholderRepository.Get(request.Id);

                if (breakEntity == null)
                {
                    throw new KeyNotFoundException($"Break with ID {request.Id} not found");
                }

                var result = _scheduleMapper.ToBreakResource(breakEntity);
                _logger.LogInformation("Successfully retrieved break with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving break with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving break with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
