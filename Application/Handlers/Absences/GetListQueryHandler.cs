using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences;

public class GetListQueryHandler : IRequestHandler<ListQuery<AbsenceResource>, IEnumerable<AbsenceResource>>
{
    private readonly AbsenceApplicationService _absenceApplicationService;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(
        AbsenceApplicationService absenceApplicationService,
        ILogger<GetListQueryHandler> logger)
    {
        _absenceApplicationService = absenceApplicationService;
        _logger = logger;
    }

    public async Task<IEnumerable<AbsenceResource>> Handle(ListQuery<AbsenceResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing get all absences query");
            
            var result = await _absenceApplicationService.GetAllAbsencesAsync(cancellationToken);
            
            _logger.LogInformation("All absences retrieved successfully");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing get all absences query");
            throw;
        }
    }
}
