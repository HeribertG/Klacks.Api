// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Adds a country/state calendar to a calendar selection and drops that selection's cached holiday calculators.
/// </summary>
/// <param name="request">Selected calendar with its owning selection id and optional official override</param>

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHolidayCalculatorCache _holidayCalculatorCache;

    public PostCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        IHolidayCalculatorCache holidayCalculatorCache,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        _holidayCalculatorCache = holidayCalculatorCache;
    }

    public async Task<SelectedCalendarResource?> Handle(PostCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var selectedCalendar = _scheduleMapper.ToSelectedCalendarEntity(request.Resource);
        await _selectedCalendarRepository.Add(selectedCalendar);
        await _unitOfWork.CompleteAsync();
        _holidayCalculatorCache.Invalidate(selectedCalendar.CalendarSelectionId);
        return _scheduleMapper.ToSelectedCalendarResource(selectedCalendar);
    }
}
