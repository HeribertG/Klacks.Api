using AutoMapper;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Services.ContainerTemplates;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class GetAvailableTasksQueryHandler : IRequestHandler<GetAvailableTasksQuery, List<ShiftResource>>
{
    private readonly ContainerAvailableTasksService _availableTasksService;
    private readonly IMapper _mapper;

    public GetAvailableTasksQueryHandler(
        ContainerAvailableTasksService availableTasksService,
        IMapper mapper)
    {
        _availableTasksService = availableTasksService;
        _mapper = mapper;
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
            request.IsWeekdayOrHoliday,
            cancellationToken);

        var resources = _mapper.Map<List<ShiftResource>>(availableTasks);
        return resources;
    }
}
