using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.WorkSchedule;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.WorkSchedule;

public class GetWorkScheduleQueryHandler : IRequestHandler<GetWorkScheduleQuery, WorkScheduleResponse>
{
    private readonly IWorkScheduleService _workScheduleService;
    private readonly IShiftGroupFilterService _groupFilterService;
    private readonly IClientWorkRepository _clientWorkRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetWorkScheduleQueryHandler> _logger;

    public GetWorkScheduleQueryHandler(
        IWorkScheduleService workScheduleService,
        IShiftGroupFilterService groupFilterService,
        IClientWorkRepository clientWorkRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetWorkScheduleQueryHandler> logger)
    {
        _workScheduleService = workScheduleService;
        _groupFilterService = groupFilterService;
        _clientWorkRepository = clientWorkRepository;
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

        var workFilter = CreateWorkFilter(request.Filter.StartDate, request.Filter.EndDate, request.Filter.SelectedGroup);
        var clients = await _clientWorkRepository.WorkList(workFilter);
        var clientResources = _scheduleMapper.ToWorkScheduleClientResourceList(clients);

        _logger.LogInformation(
            "Returned {EntryCount} work schedule entries and {ClientCount} clients",
            resources.Count,
            clientResources.Count);

        return new WorkScheduleResponse
        {
            Entries = resources,
            Clients = clientResources
        };
    }

    private static WorkFilter CreateWorkFilter(DateOnly startDate, DateOnly endDate, Guid? selectedGroup)
    {
        var firstDayOfMonth = new DateOnly(startDate.Year, startDate.Month, 1);
        var lastDayOfMonth = new DateOnly(endDate.Year, endDate.Month, DateTime.DaysInMonth(endDate.Year, endDate.Month));

        return new WorkFilter
        {
            CurrentYear = startDate.Year,
            CurrentMonth = startDate.Month,
            DayVisibleBeforeMonth = (firstDayOfMonth.ToDateTime(TimeOnly.MinValue) - startDate.ToDateTime(TimeOnly.MinValue)).Days,
            DayVisibleAfterMonth = (endDate.ToDateTime(TimeOnly.MinValue) - lastDayOfMonth.ToDateTime(TimeOnly.MinValue)).Days,
            SelectedGroup = selectedGroup,
            SearchString = string.Empty
        };
    }
}
