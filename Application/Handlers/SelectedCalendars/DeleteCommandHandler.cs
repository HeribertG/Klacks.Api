// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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

    public DeleteCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
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
        return selectedCalendarResource;
    }
}
