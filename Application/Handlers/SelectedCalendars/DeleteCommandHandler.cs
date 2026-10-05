// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Removes a selected calendar from its calendar selection and drops that selection's cached holiday calculators.
/// </summary>
/// <param name="request">Id of the selected calendar to delete</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHolidayCalculatorCache _holidayCalculatorCache;

    public DeleteCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IHolidayCalculatorCache holidayCalculatorCache,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _holidayCalculatorCache = holidayCalculatorCache;
    }

    public async Task<SelectedCalendarResource?> Handle(DeleteCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var existingSelectedCalendar = await _selectedCalendarRepository.Get(request.Id);
        if (existingSelectedCalendar == null)
        {
            return null;
        }

        var selectedCalendarResource = _scheduleMapper.ToSelectedCalendarResource(existingSelectedCalendar);
        await _selectedCalendarRepository.Delete(request.Id);
        await _unitOfWork.CompleteAsync();
        _holidayCalculatorCache.Invalidate(existingSelectedCalendar.CalendarSelectionId);
        return selectedCalendarResource;
    }
}
