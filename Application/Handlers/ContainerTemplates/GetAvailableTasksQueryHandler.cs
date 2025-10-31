using AutoMapper;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.ContainerTemplates;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.ContainerTemplates;

public class GetAvailableTasksQueryHandler : IRequestHandler<GetAvailableTasksQuery, List<ShiftResource>>
{
    private readonly IShiftRepository _shiftRepository;
    private readonly DataBaseContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAvailableTasksQueryHandler> _logger;

    public GetAvailableTasksQueryHandler(
        IShiftRepository shiftRepository,
        DataBaseContext context,
        IMapper mapper,
        ILogger<GetAvailableTasksQueryHandler> logger)
    {
        _shiftRepository = shiftRepository;
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<ShiftResource>> Handle(GetAvailableTasksQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting available tasks for container: {ContainerId}, weekdays: {Weekdays}, timeframe: {FromTime}-{UntilTime}, searchString: {SearchString}, excluding container: {ExcludeContainerId}, isHoliday: {IsHoliday}, isWeekdayOrHoliday: {IsWeekdayOrHoliday}",
            request.ContainerId, string.Join(",", request.Weekdays), request.FromTime, request.UntilTime, request.SearchString, request.ExcludeContainerId, request.IsHoliday, request.IsWeekdayOrHoliday);

        var containerGroupIds = await _context.GroupItem
            .Where(gi => gi.ShiftId == request.ContainerId)
            .Select(gi => gi.GroupId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Container shift belongs to {Count} groups", containerGroupIds.Count);

        var usedShiftIds = await _context.ContainerTemplateItem
            .Where(cti => request.ExcludeContainerId == null ||
                          cti.ContainerTemplate.ContainerId != request.ExcludeContainerId)
            .Select(cti => cti.ShiftId)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} shifts already used in containers", usedShiftIds.Count);

        var shiftsInSameGroups = await _context.GroupItem
            .Where(gi => containerGroupIds.Contains(gi.GroupId) && gi.ShiftId != null)
            .Select(gi => gi.ShiftId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} shifts in the same groups", shiftsInSameGroups.Count);

        var query = _shiftRepository.GetQuery()
            .Where(s => s.ShiftType == ShiftType.IsTask)
            .Where(s => s.Status >= ShiftStatus.OriginalShift)
            .Where(s => s.StartShift >= request.FromTime && s.EndShift <= request.UntilTime)
            .Where(s => !usedShiftIds.Contains(s.Id))
            .Where(s => shiftsInSameGroups.Contains(s.Id));

        query = ApplyWeekdayFilter(query, request.Weekdays);

        if (!string.IsNullOrWhiteSpace(request.SearchString))
        {
            var searchLower = request.SearchString.ToLower();
            query = query.Where(s =>
                s.Name.ToLower().Contains(searchLower) ||
                s.Abbreviation.ToLower().Contains(searchLower) ||
                s.Description.ToLower().Contains(searchLower));
        }

        if (request.IsHoliday.HasValue)
        {
            query = query.Where(s => s.IsHoliday == request.IsHoliday.Value);
        }

        if (request.IsWeekdayOrHoliday.HasValue)
        {
            query = query.Where(s => s.IsWeekdayOrHoliday == request.IsWeekdayOrHoliday.Value);
        }

        var availableTasks = await query
            .Include(s => s.Client)
            .AsNoTracking()
            .OrderBy(s => s.StartShift)
            .ThenBy(s => s.Client!.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Found {Count} available tasks", availableTasks.Count);

        var resources = _mapper.Map<List<ShiftResource>>(availableTasks);
        return resources;
    }

    private IQueryable<Shift> ApplyWeekdayFilter(IQueryable<Shift> query, int[] weekdays)
    {
        if (weekdays == null || weekdays.Length == 0)
        {
            return query;
        }

        return query.Where(s =>
            (weekdays.Contains(0) && s.IsSunday) ||
            (weekdays.Contains(1) && s.IsMonday) ||
            (weekdays.Contains(2) && s.IsTuesday) ||
            (weekdays.Contains(3) && s.IsWednesday) ||
            (weekdays.Contains(4) && s.IsThursday) ||
            (weekdays.Contains(5) && s.IsFriday) ||
            (weekdays.Contains(6) && s.IsSaturday));
    }
}
