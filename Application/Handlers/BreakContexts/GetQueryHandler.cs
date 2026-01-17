using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.BreakContexts;

public class GetQueryHandler : IRequestHandler<GetQuery<BreakContextResource>, BreakContextResource>
{
    private readonly IBreakContextRepository _breakContextRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IBreakContextRepository breakContextRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
    {
        _breakContextRepository = breakContextRepository;
        _settingsMapper = settingsMapper;
        _logger = logger;
    }

    public async Task<BreakContextResource> Handle(GetQuery<BreakContextResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting break context with ID: {Id}", request.Id);

            var breakContext = await _breakContextRepository.Get(request.Id);

            if (breakContext == null)
            {
                throw new KeyNotFoundException($"BreakContext with ID {request.Id} not found");
            }

            var result = _settingsMapper.ToBreakContextResource(breakContext);
            _logger.LogInformation("Successfully retrieved break context with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving break context with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving break context with ID {request.Id}: {ex.Message}");
        }
    }
}
