// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.ScheduleEntries;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Services.ShiftSchedule;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ScheduleEntries;

public class GetScheduleEntriesQueryHandler : IRequestHandler<GetScheduleEntriesQuery, WorkScheduleResponse>
{
    private readonly IScheduleEntriesService _scheduleEntriesService;
    private readonly IShiftGroupFilterService _groupFilterService;
    private readonly IWorkRepository _workRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IClientAvailabilityScheduleService _clientAvailabilityScheduleService;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly ILogger<GetScheduleEntriesQueryHandler> _logger;

    public GetScheduleEntriesQueryHandler(
        IScheduleEntriesService scheduleEntriesService,
        IShiftGroupFilterService groupFilterService,
        IWorkRepository workRepository,
        ScheduleMapper scheduleMapper,
        IClientAvailabilityScheduleService clientAvailabilityScheduleService,
        IGroupItemRepository groupItemRepository,
        ILogger<GetScheduleEntriesQueryHandler> logger)
    {
        _scheduleEntriesService = scheduleEntriesService;
        _groupFilterService = groupFilterService;
        _workRepository = workRepository;
        _scheduleMapper = scheduleMapper;
        _clientAvailabilityScheduleService = clientAvailabilityScheduleService;
        _groupItemRepository = groupItemRepository;
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

        var workFilter = CreateWorkFilter(request.Filter, startDate, endDate);

        var (clients, totalClientCount) = await _workRepository.WorkList(workFilter, cancellationToken);
        var clientResources = _scheduleMapper.ToWorkScheduleClientResourceList(clients);
        EnrichWithContractInfo(clientResources, clients, startDate, endDate, request.Filter.PaymentInterval);

        var clientIds = clients.Select(c => c.Id).ToList();

        var visibleGroupIds = await _groupFilterService.GetVisibleGroupIdsAsync(
            request.Filter.SelectedGroup);

        await EnrichWithGroupItemInfo(clientResources, clientIds, visibleGroupIds, cancellationToken);

        var entries = await _scheduleEntriesService.GetScheduleEntriesQuery(
            startDate,
            endDate,
            visibleGroupIds.Count > 0 ? visibleGroupIds : null,
            request.Filter.AnalyseToken)
            .Where(e => clientIds.Contains(e.ClientId))
            .ToListAsync(cancellationToken);

        var resources = _scheduleMapper.ToWorkScheduleResourceList(entries);

        var availabilityEntries = await _clientAvailabilityScheduleService
            .GetClientAvailabilityQuery(startDate, endDate, clientIds)
            .ToListAsync(cancellationToken);

        var clientAvailabilities = availabilityEntries
            .GroupBy(a => a.ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(
                    a => DateOnly.FromDateTime(a.AvailabilityDate),
                    a => a.AvailabilityRanges));

        var periodHoursStart = request.Filter.PeriodStartDate ?? startDate;
        var periodHoursEnd = request.Filter.PeriodEndDate ?? endDate;

        var periodHours = await _workRepository.GetPeriodHoursForClients(
            clientIds,
            periodHoursStart,
            periodHoursEnd,
            request.Filter.AnalyseToken,
            cancellationToken);

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
            EndDate = endDate,
            ClientAvailabilities = clientAvailabilities
        };
    }

    internal static WorkFilter CreateWorkFilter(
        Application.DTOs.Filter.WorkScheduleFilter filter,
        DateOnly startDate,
        DateOnly endDate)
    {
        return new WorkFilter
        {
            StartDate = startDate,
            EndDate = endDate,
            SelectedGroup = filter.SelectedGroup,
            SearchString = filter.SearchString,
            OrderBy = filter.OrderBy,
            SortOrder = filter.SortOrder,
            ShowEmployees = filter.ShowEmployees,
            ShowExtern = filter.ShowExtern,
            IndividualSort = filter.IndividualSort,
            StartRow = filter.StartRow,
            RowCount = filter.RowCount,
            PaymentInterval = filter.PaymentInterval,
            ClientId = filter.ClientId,
            ClientIds = filter.ClientIds
        };
    }

    private async Task EnrichWithGroupItemInfo(
        List<WorkScheduleClientResource> clientResources,
        List<Guid> clientIds,
        List<Guid> visibleGroupIds,
        CancellationToken cancellationToken)
    {
        if (visibleGroupIds.Count == 0) return;

        var groupItems = await _groupItemRepository.GetQuery()
            .Where(gi => !gi.IsDeleted
                         && gi.ClientId.HasValue
                         && clientIds.Contains(gi.ClientId.Value)
                         && visibleGroupIds.Contains(gi.GroupId))
            .Select(gi => new { gi.ClientId, gi.ValidFrom, gi.ValidUntil })
            .ToListAsync(cancellationToken);

        var rangeByClient = groupItems
            .GroupBy(gi => gi.ClientId!.Value)
            .ToDictionary(
                g => g.Key,
                g => (
                    ValidFrom: g.Min(gi => gi.ValidFrom),
                    ValidUntil: g.Max(gi => gi.ValidUntil)
                ));

        foreach (var resource in clientResources)
        {
            if (rangeByClient.TryGetValue(resource.Id, out var range))
            {
                resource.GroupItemValidFrom = range.ValidFrom.HasValue
                    ? DateOnly.FromDateTime(range.ValidFrom.Value) : null;
                resource.GroupItemValidUntil = range.ValidUntil.HasValue
                    ? DateOnly.FromDateTime(range.ValidUntil.Value) : null;
            }
        }
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

            if (client.Membership?.ValidFrom is { } validFrom)
                resource.MemberSince = DateOnly.FromDateTime(validFrom);
        }
    }
}
