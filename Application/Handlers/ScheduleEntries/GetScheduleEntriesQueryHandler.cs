using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ScheduleEntries;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ScheduleEntries;

public class GetScheduleEntriesQueryHandler : IRequestHandler<GetScheduleEntriesQuery, WorkScheduleResponse>
{
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IShiftGroupFilterService _groupFilterService;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly ILogger<GetScheduleEntriesQueryHandler> _logger;

    public GetScheduleEntriesQueryHandler(
        IScheduleEntriesService scheduleEntriesService,
        IShiftGroupFilterService groupFilterService,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        ILogger<GetScheduleEntriesQueryHandler> logger)
    {
        _scheduleEntriesService = scheduleEntriesService;
        _groupFilterService = groupFilterService;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _logger = logger;
    }

    public async Task<WorkScheduleResponse> Handle(
        GetScheduleEntriesQuery request,
        CancellationToken cancellationToken)
    {
        var startDate = request.Filter.StartDate;
        var endDate = request.Filter.EndDate;

        if (startDate == DateOnly.MinValue || endDate == DateOnly.MinValue)
        {
            _logger.LogError("Invalid dates received - using current month as fallback");
            var now = DateTime.UtcNow;
            startDate = new DateOnly(now.Year, now.Month, 1).AddDays(-10);
            endDate = new DateOnly(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)).AddDays(10);
        }

        _logger.LogInformation(
            "Handling GetScheduleEntriesQuery - StartDate: {StartDate}, EndDate: {EndDate}, StartRow: {StartRow}, RowCount: {RowCount}",
            startDate,
            endDate,
            request.Filter.StartRow,
            request.Filter.RowCount);

        var workFilter = CreateWorkFilter(
            startDate,
            endDate,
            request.Filter.SelectedGroup,
            request.Filter.OrderBy,
            request.Filter.SortOrder,
            request.Filter.ShowEmployees,
            request.Filter.ShowExtern,
            request.Filter.HoursSortOrder,
            request.Filter.StartRow,
            request.Filter.RowCount,
            request.Filter.PaymentInterval);

        var (clients, totalClientCount) = await _workRepository.WorkList(workFilter);
        var clientResources = _scheduleMapper.ToWorkScheduleClientResourceList(clients);
        EnrichWithContractInfo(clientResources, clients, startDate, endDate, request.Filter.PaymentInterval);

        var clientIds = clients.Select(c => c.Id).ToList();

        var visibleGroupIds = await _groupFilterService.GetVisibleGroupIdsAsync(
            request.Filter.SelectedGroup);

        var entries = await _scheduleEntriesService.GetScheduleEntriesQuery(
            startDate,
            endDate,
            visibleGroupIds.Count > 0 ? visibleGroupIds : null)
            .Where(e => clientIds.Contains(e.ClientId))
            .ToListAsync(cancellationToken);

        var resources = _scheduleMapper.ToWorkScheduleResourceList(entries);

        var periodHours = await _workRepository.GetPeriodHoursForClients(
            clientIds,
            startDate,
            endDate);

        _logger.LogInformation(
            "Returned {EntryCount} schedule entries and {ClientCount} clients (total: {TotalCount})",
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
        int rowCount,
        int paymentInterval)
    {
        return new WorkFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            SelectedGroup = selectedGroup,
            SearchString = string.Empty,
            OrderBy = orderBy,
            SortOrder = sortOrder,
            ShowEmployees = showEmployees,
            ShowExtern = showExtern,
            HoursSortOrder = hoursSortOrder,
            StartRow = startRow,
            RowCount = rowCount,
            PaymentInterval = paymentInterval
        };
    }

    private static void EnrichWithContractInfo(
        List<WorkScheduleClientResource> clientResources,
        List<Domain.Models.Staffs.Client> clients,
        DateOnly startDate,
        DateOnly endDate,
        int paymentInterval)
    {
        foreach (var resource in clientResources)
        {
            var client = clients.FirstOrDefault(c => c.Id == resource.Id);
            if (client?.ClientContracts == null || !client.ClientContracts.Any())
            {
                resource.HasContract = false;
                continue;
            }

            var activeContract = client.ClientContracts
                .Where(cc => cc.IsActive && !cc.IsDeleted)
                .Where(cc => cc.FromDate <= endDate && (!cc.UntilDate.HasValue || cc.UntilDate >= startDate))
                .Where(cc => cc.Contract != null && (int)cc.Contract.PaymentInterval == paymentInterval)
                .OrderByDescending(cc => cc.FromDate)
                .FirstOrDefault();

            resource.HasContract = activeContract != null;
        }
    }
}
