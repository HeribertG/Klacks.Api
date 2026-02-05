using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ISelectedCalendarRepository _selectedCalendarRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SelectedCalendarResource?> Handle(PutCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var existingSelectedCalendar = await _selectedCalendarRepository.Get(request.Resource.Id);
        if (existingSelectedCalendar == null)
        {
            return null;
        }

        var updatedSelectedCalendar = _scheduleMapper.ToSelectedCalendarEntity(request.Resource);
        updatedSelectedCalendar.CreateTime = existingSelectedCalendar.CreateTime;
        updatedSelectedCalendar.CurrentUserCreated = existingSelectedCalendar.CurrentUserCreated;
        existingSelectedCalendar = updatedSelectedCalendar;
        await _selectedCalendarRepository.Put(existingSelectedCalendar);
        await _unitOfWork.CompleteAsync();
        return _scheduleMapper.ToSelectedCalendarResource(existingSelectedCalendar);
    }
}
