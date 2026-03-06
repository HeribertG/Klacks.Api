// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ISettingsRepository _settingsRepository;
    private readonly DataBaseContext _context;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        ISettingsRepository settingsRepository,
        DataBaseContext context,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _settingsRepository = settingsRepository;
        _context = context;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<CalendarSelectionResource?> Handle(DeleteCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existingCalendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(request.Id);
            if (existingCalendarSelection == null)
            {
                throw new KeyNotFoundException($"Calendar Selection with ID {request.Id} not found.");
            }

            var usages = await GetUsagesAsync(request.Id);
            if (usages.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Calendar Selection '{existingCalendarSelection.Name}' is in use by: {string.Join(", ", usages)}. Remove these references first.");
            }

            await _calendarSelectionRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToCalendarSelectionResource(existingCalendarSelection);
        },
        "deleting calendar selection",
        new { CalendarSelectionId = request.Id });
    }

    private async Task<List<string>> GetUsagesAsync(Guid calendarSelectionId)
    {
        var usages = new List<string>();

        var globalSetting = await _settingsRepository.GetSetting(Domain.Constants.SettingKeys.GlobalCalendarSelectionId);
        if (globalSetting?.Value == calendarSelectionId.ToString())
        {
            usages.Add("Global Settings");
        }

        var groupCount = await _context.Set<Group>()
            .CountAsync(g => !g.IsDeleted && g.CalendarSelectionId == calendarSelectionId);
        if (groupCount > 0)
        {
            usages.Add($"{groupCount} Group(s)");
        }

        var contractCount = await _context.Set<Contract>()
            .CountAsync(c => !c.IsDeleted && c.CalendarSelectionId == calendarSelectionId);
        if (contractCount > 0)
        {
            usages.Add($"{contractCount} Contract(s)");
        }

        return usages;
    }
}
