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

    public PostCommandHandler(
        ISelectedCalendarRepository selectedCalendarRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _selectedCalendarRepository = selectedCalendarRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<SelectedCalendarResource?> Handle(PostCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        var selectedCalendar = _scheduleMapper.ToSelectedCalendarEntity(request.Resource);
        await _selectedCalendarRepository.Add(selectedCalendar);
        await _unitOfWork.CompleteAsync();
        return _scheduleMapper.ToSelectedCalendarResource(selectedCalendar);
    }
}
