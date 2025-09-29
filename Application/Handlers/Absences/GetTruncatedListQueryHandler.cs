using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;

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
                
                if (request.Filter == null)
                {
                    _logger.LogWarning("Filter parameter is null for truncated absence list query");
                    throw new InvalidRequestException("Filter parameter is required for truncated list query");
                }
                
                var result = await _absenceRepository.Truncated(request.Filter);
                
                if (result == null)
                {
                    _logger.LogWarning("No truncated absence data found for the provided filter");
                    throw new KeyNotFoundException("No absence data found for the specified filter criteria");
                }
                
                _logger.LogInformation("Truncated absence list retrieved successfully with {Count} items", result.MaxItems);
                return result;
            }
            catch (InvalidRequestException)
            {
                throw;
            }
            catch (KeyNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while retrieving truncated absence list");
                throw new InvalidRequestException($"Failed to retrieve truncated absence list: {ex.Message}");
            }
    }
    }
}
