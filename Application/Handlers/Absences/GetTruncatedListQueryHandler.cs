using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences
{
    public class GetTruncatedListQueryHandler : IRequestHandler<TruncatedListQuery, TruncatedAbsence>
    {
        private readonly AbsenceApplicationService _absenceApplicationService;
        private readonly ILogger<GetTruncatedListQueryHandler> _logger;

        public GetTruncatedListQueryHandler(
            AbsenceApplicationService absenceApplicationService,
            ILogger<GetTruncatedListQueryHandler> logger)
        {
            _absenceApplicationService = absenceApplicationService;
            _logger = logger;
        }

        public async Task<TruncatedAbsence> Handle(TruncatedListQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing truncated absence list query");
                
                var result = await _absenceApplicationService.GetTruncatedAbsencesAsync(request.Filter, cancellationToken);
                
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
