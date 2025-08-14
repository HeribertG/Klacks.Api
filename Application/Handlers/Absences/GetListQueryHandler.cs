using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Absences;

public class GetListQueryHandler : IRequestHandler<ListQuery<AbsenceResource>, IEnumerable<AbsenceResource>>
{
    private readonly IAbsenceRepository _absenceRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<GetListQueryHandler> _logger;

    public GetListQueryHandler(
        IAbsenceRepository absenceRepository,
        IMapper mapper,
        ILogger<GetListQueryHandler> logger)
    {
        _absenceRepository = absenceRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<AbsenceResource>> Handle(ListQuery<AbsenceResource> request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Processing get all absences query");
            
            var absences = await _absenceRepository.List();
            var result = _mapper.Map<IEnumerable<AbsenceResource>>(absences);
            
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
