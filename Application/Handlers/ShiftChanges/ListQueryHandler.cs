using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;

namespace Klacks.Api.Application.Handlers.ShiftChanges;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<ShiftChangeResource>, IEnumerable<ShiftChangeResource>>
{
    private readonly IShiftChangeRepository _shiftChangeRepository;
    private readonly ScheduleMapper _scheduleMapper;

    public ListQueryHandler(
        IShiftChangeRepository shiftChangeRepository,
        ScheduleMapper scheduleMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _shiftChangeRepository = shiftChangeRepository;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<IEnumerable<ShiftChangeResource>> Handle(ListQuery<ShiftChangeResource> request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all ShiftChanges");

        var shiftChanges = await _shiftChangeRepository.List();

        _logger.LogInformation("Successfully retrieved {Count} ShiftChanges", shiftChanges.Count);
        return _scheduleMapper.ToShiftChangeResourceList(shiftChanges);
    }
}
