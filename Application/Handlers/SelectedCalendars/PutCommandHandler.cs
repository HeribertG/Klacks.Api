// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Updates a selected calendar (country/state, official override, owning selection) and drops the cached holiday
/// calculators of the previous and the new owning selection.
/// </summary>
/// <param name="request">Selected calendar resource; returns null when the id is unknown</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHolidayCalculatorCache _holidayCalculatorCache;

    public PutCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IHolidayCalculatorCache holidayCalculatorCache,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _holidayCalculatorCache = holidayCalculatorCache;
    }

    public async Task<SelectedCalendarResource?> Handle(PutCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var existingSelectedCalendar = await _selectedCalendarRepository.Get(request.Resource.Id);
        if (existingSelectedCalendar == null)
        {
            return null;
        }

        var previousCalendarSelectionId = existingSelectedCalendar.CalendarSelectionId;
        var updatedSelectedCalendar = _scheduleMapper.ToSelectedCalendarEntity(request.Resource);
        updatedSelectedCalendar.CreateTime = existingSelectedCalendar.CreateTime;
        updatedSelectedCalendar.CurrentUserCreated = existingSelectedCalendar.CurrentUserCreated;
        existingSelectedCalendar = updatedSelectedCalendar;
        await _selectedCalendarRepository.Put(existingSelectedCalendar);
        await _unitOfWork.CompleteAsync();
        _holidayCalculatorCache.Invalidate(previousCalendarSelectionId);
        if (existingSelectedCalendar.CalendarSelectionId != previousCalendarSelectionId)
        {
            _holidayCalculatorCache.Invalidate(existingSelectedCalendar.CalendarSelectionId);
        }
        return _scheduleMapper.ToSelectedCalendarResource(existingSelectedCalendar);
    }
}
