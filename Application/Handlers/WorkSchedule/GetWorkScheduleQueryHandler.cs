using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.WorkSchedule;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.WorkSchedule;

public class GetWorkScheduleQueryHandler : IRequestHandler<GetWorkScheduleQuery, WorkScheduleResponse>
{
    private readonly IWorkScheduleService _workScheduleService;
    private readonly IShiftGroupFilterService _groupFilterService;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetWorkScheduleQueryHandler> _logger;

    public GetWorkScheduleQueryHandler(
        IWorkScheduleService workScheduleService,
        IShiftGroupFilterService groupFilterService,
        ScheduleMapper scheduleMapper,
        ILogger<GetWorkScheduleQueryHandler> logger)
    {
        _workScheduleService = workScheduleService;
        _groupFilterService = groupFilterService;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<WorkScheduleResponse> Handle(
        GetWorkScheduleQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetWorkScheduleQuery from {StartDate} to {EndDate}",
            request.Filter.StartDate,
            request.Filter.EndDate);

        var visibleGroupIds = await _groupFilterService.GetVisibleGroupIdsAsync(
            request.Filter.SelectedGroup);

        var entries = await _workScheduleService.GetWorkScheduleQuery(
            request.Filter.StartDate,
            request.Filter.EndDate,
            visibleGroupIds.Count > 0 ? visibleGroupIds : null)
            .ToListAsync(cancellationToken);

        var resources = _scheduleMapper.ToWorkScheduleResourceList(entries);

        _logger.LogInformation("Returned {Count} work schedule entries", resources.Count);

        return new WorkScheduleResponse
        {
            Entries = resources
        };
    }
}
