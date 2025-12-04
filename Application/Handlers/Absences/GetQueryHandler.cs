using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetQueryHandler : IRequestHandler<GetQuery<AbsenceResource>, AbsenceResource>
    {
        private readonly IAbsenceRepository _absenceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetQueryHandler> _logger;

        public GetQueryHandler(IAbsenceRepository absenceRepository, IMapper mapper, ILogger<GetQueryHandler> logger)
        {
            _absenceRepository = absenceRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<AbsenceResource> Handle(GetQuery<AbsenceResource> request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting absence with ID: {Id}", request.Id);
                
                var absence = await _absenceRepository.Get(request.Id);
                
                if (absence == null)
                {
                    throw new KeyNotFoundException($"Absence with ID {request.Id} not found");
                }
                
                var result = _mapper.Map<AbsenceResource>(absence)!;
                _logger.LogInformation("Successfully retrieved absence with ID: {Id}", request.Id);
                return result;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving absence with ID: {Id}", request.Id);
                throw new InvalidRequestException($"Error retrieving absence with ID {request.Id}: {ex.Message}");
            }
        }
    }
}
