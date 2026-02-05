using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.AbsenceDetails;

public class GetQueryHandler : IRequestHandler<GetQuery<AbsenceDetailResource>, AbsenceDetailResource>
{
    private readonly IAbsenceDetailRepository _absenceDetailRepository;
    private readonly SettingsMapper _settingsMapper;
    private readonly ILogger<GetQueryHandler> _logger;

    public GetQueryHandler(IAbsenceDetailRepository absenceDetailRepository, SettingsMapper settingsMapper, ILogger<GetQueryHandler> logger)
    {
        _absenceDetailRepository = absenceDetailRepository;
        _settingsMapper = settingsMapper;
        _logger = logger;
    }

    public async Task<AbsenceDetailResource> Handle(GetQuery<AbsenceDetailResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting absence detail with ID: {Id}", request.Id);

            var absenceDetail = await _absenceDetailRepository.Get(request.Id);

            if (absenceDetail == null)
            {
                throw new KeyNotFoundException($"AbsenceDetail with ID {request.Id} not found");
            }

            var result = _settingsMapper.ToAbsenceDetailResource(absenceDetail);
            _logger.LogInformation("Successfully retrieved absence detail with ID: {Id}", request.Id);
            return result;
        }
        catch (KeyNotFoundException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving absence detail with ID: {Id}", request.Id);
            throw new InvalidRequestException($"Error retrieving absence detail with ID {request.Id}: {ex.Message}");
        }
    }
}
