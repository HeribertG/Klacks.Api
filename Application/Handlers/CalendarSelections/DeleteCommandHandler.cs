// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
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

            await _calendarSelectionRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return _scheduleMapper.ToCalendarSelectionResource(existingCalendarSelection);
        }, 
        "deleting calendar selection", 
        new { CalendarSelectionId = request.Id });
    }
}
