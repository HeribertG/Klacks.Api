using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class GetAvailableTasksQueryHandler : IRequestHandler<GetAvailableTasksQuery, List<ShiftResource>>
{
    private readonly IContainerAvailableTasksService _availableTasksService;
    private readonly ScheduleMapper _scheduleMapper;

    public GetAvailableTasksQueryHandler(
        IContainerAvailableTasksService availableTasksService,
        ScheduleMapper scheduleMapper)
    {
        _availableTasksService = availableTasksService;
        _scheduleMapper = scheduleMapper;
    }

    public async Task<List<ShiftResource>> Handle(GetAvailableTasksQuery request, CancellationToken cancellationToken)
    {
        var availableTasks = await _availableTasksService.GetAvailableTasksAsync(
            request.ContainerId,
            request.Weekday,
            request.FromTime,
            request.UntilTime,
            request.SearchString,
            request.ExcludeContainerId,
            request.IsHoliday,
            request.IsWeekdayAndHoliday,
            cancellationToken);

        var resources = availableTasks.Select(s => _scheduleMapper.ToShiftResource(s)).ToList();
        return resources;
    }
}
