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
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetWorkScheduleQueryHandler> _logger;

    public GetWorkScheduleQueryHandler(
        IWorkScheduleService workScheduleService,
        IShiftGroupFilterService groupFilterService,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetWorkScheduleQueryHandler> logger)
    {
        _workScheduleService = workScheduleService;
        _groupFilterService = groupFilterService;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<WorkScheduleResponse> Handle(
        GetWorkScheduleQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling GetWorkScheduleQuery from {StartDate} to {EndDate}, StartRow: {StartRow}, RowCount: {RowCount}",
            request.Filter.StartDate,
            request.Filter.EndDate,
            request.Filter.StartRow,
            request.Filter.RowCount);

        var workFilter = CreateWorkFilter(
            request.Filter.StartDate,
            request.Filter.EndDate,
            request.Filter.SelectedGroup,
            request.Filter.OrderBy,
            request.Filter.SortOrder,
            request.Filter.ShowEmployees,
            request.Filter.ShowExtern,
            request.Filter.HoursSortOrder,
            request.Filter.StartRow,
            request.Filter.RowCount);

        var (clients, totalClientCount) = await _workRepository.WorkList(workFilter);
        var clientResources = _scheduleMapper.ToWorkScheduleClientResourceList(clients);

        var clientIds = clients.Select(c => c.Id).ToList();

        var visibleGroupIds = await _groupFilterService.GetVisibleGroupIdsAsync(
            request.Filter.SelectedGroup);

        var entries = await _workScheduleService.GetWorkScheduleQuery(
            request.Filter.StartDate,
            request.Filter.EndDate,
            visibleGroupIds.Count > 0 ? visibleGroupIds : null)
            .Where(e => clientIds.Contains(e.ClientId))
            .ToListAsync(cancellationToken);

        var resources = _scheduleMapper.ToWorkScheduleResourceList(entries);

        var monthlyHours = await _workRepository.GetMonthlyHoursForClients(
            clientIds,
            request.Filter.StartDate.Year,
            request.Filter.StartDate.Month);

        _logger.LogInformation(
            "Returned {EntryCount} work schedule entries and {ClientCount} clients (total: {TotalCount})",
            resources.Count,
            clientResources.Count,
            totalClientCount);

        return new WorkScheduleResponse
        {
            Entries = resources,
            Clients = clientResources,
            MonthlyHours = monthlyHours,
            TotalClientCount = totalClientCount
        };
    }

    private static WorkFilter CreateWorkFilter(
        DateOnly startDate,
        DateOnly endDate,
        Guid? selectedGroup,
        string orderBy,
        string sortOrder,
        bool showEmployees,
        bool showExtern,
        string? hoursSortOrder,
        int startRow,
        int rowCount)
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
            SearchString = string.Empty,
            OrderBy = orderBy,
            SortOrder = sortOrder,
            ShowEmployees = showEmployees,
            ShowExtern = showExtern,
            HoursSortOrder = hoursSortOrder,
            StartRow = startRow,
            RowCount = rowCount
        };
    }
}
