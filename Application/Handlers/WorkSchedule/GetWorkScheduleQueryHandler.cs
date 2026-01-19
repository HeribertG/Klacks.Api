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
    private readonly ISettingsRepository _settingsRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetWorkScheduleQueryHandler> _logger;

    public GetWorkScheduleQueryHandler(
        IWorkScheduleService workScheduleService,
        IShiftGroupFilterService groupFilterService,
        IWorkRepository workRepository,
        ISettingsRepository settingsRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetWorkScheduleQueryHandler> logger)
    {
        _workScheduleService = workScheduleService;
        _groupFilterService = groupFilterService;
        _workRepository = workRepository;
        _settingsRepository = settingsRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<WorkScheduleResponse> Handle(
        GetWorkScheduleQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Received filter - PeriodStartDate: {PeriodStartDate}, PeriodEndDate: {PeriodEndDate}",
            request.Filter.PeriodStartDate,
            request.Filter.PeriodEndDate);

        var dayVisibleBefore = await GetSettingAsInt("dayVisibleBeforeMonth", 10);
        var dayVisibleAfter = await GetSettingAsInt("dayVisibleAfterMonth", 10);

        _logger.LogInformation(
            "Settings - dayVisibleBefore: {DayVisibleBefore}, dayVisibleAfter: {DayVisibleAfter}",
            dayVisibleBefore,
            dayVisibleAfter);

        var periodStartDate = request.Filter.PeriodStartDate;
        var periodEndDate = request.Filter.PeriodEndDate;

        if (periodStartDate == DateOnly.MinValue || periodEndDate == DateOnly.MinValue)
        {
            _logger.LogError("Invalid period dates received - using current month as fallback");
            var now = DateTime.UtcNow;
            periodStartDate = new DateOnly(now.Year, now.Month, 1);
            periodEndDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
        }

        var startDate = periodStartDate.AddDays(-dayVisibleBefore);
        var endDate = periodEndDate.AddDays(dayVisibleAfter);

        _logger.LogInformation(
            "Handling GetWorkScheduleQuery Period: {PeriodStart} to {PeriodEnd}, Visible: {StartDate} to {EndDate}, StartRow: {StartRow}, RowCount: {RowCount}",
            periodStartDate,
            periodEndDate,
            startDate,
            endDate,
            request.Filter.StartRow,
            request.Filter.RowCount);

        var workFilter = CreateWorkFilter(
            startDate,
            endDate,
            periodStartDate,
            dayVisibleBefore,
            dayVisibleAfter,
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
            startDate,
            endDate,
            visibleGroupIds.Count > 0 ? visibleGroupIds : null)
            .Where(e => clientIds.Contains(e.ClientId))
            .ToListAsync(cancellationToken);

        var resources = _scheduleMapper.ToWorkScheduleResourceList(entries);

        var periodHours = await _workRepository.GetPeriodHoursForClients(
            clientIds,
            periodStartDate,
            periodEndDate);

        _logger.LogInformation(
            "Returned {EntryCount} work schedule entries and {ClientCount} clients (total: {TotalCount})",
            resources.Count,
            clientResources.Count,
            totalClientCount);

        return new WorkScheduleResponse
        {
            Entries = resources,
            Clients = clientResources,
            PeriodHours = periodHours,
            TotalClientCount = totalClientCount,
            StartDate = startDate,
            EndDate = endDate
        };
    }

    private async Task<int> GetSettingAsInt(string key, int defaultValue)
    {
        var setting = await _settingsRepository.GetSetting(key);
        if (setting != null && int.TryParse(setting.Value, out var value))
        {
            return value;
        }
        return defaultValue;
    }

    private static WorkFilter CreateWorkFilter(
        DateOnly startDate,
        DateOnly endDate,
        DateOnly periodStartDate,
        int dayVisibleBefore,
        int dayVisibleAfter,
        Guid? selectedGroup,
        string orderBy,
        string sortOrder,
        bool showEmployees,
        bool showExtern,
        string? hoursSortOrder,
        int startRow,
        int rowCount)
    {
        return new WorkFilter
        {
            CurrentYear = periodStartDate.Year,
            CurrentMonth = periodStartDate.Month,
            DayVisibleBeforeMonth = dayVisibleBefore,
            DayVisibleAfterMonth = dayVisibleAfter,
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
