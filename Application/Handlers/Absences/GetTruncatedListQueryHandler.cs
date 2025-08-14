using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetTruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedAbsence>
    {
        private readonly IAbsenceRepository _absenceRepository;
        private readonly ILogger<GetTruncatedListQueryHandler> _logger;

        public GetTruncatedListQueryHandler(
            IAbsenceRepository absenceRepository,
            ILogger<GetTruncatedListQueryHandler> logger)
        {
            _absenceRepository = absenceRepository;
            _logger = logger;
        }

        public async Task<TruncatedAbsence> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing truncated absence list query");
                
                var result = await _absenceRepository.Truncated(request.Filter);
                
                _logger.LogInformation("Truncated absence list retrieved successfully");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing truncated absence list query");
                throw;
            }
        }
    }
}
