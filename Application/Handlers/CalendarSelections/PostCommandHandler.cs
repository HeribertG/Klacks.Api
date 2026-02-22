using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly ScheduleMapper _scheduleMapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PostCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        ScheduleMapper scheduleMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _scheduleMapper = scheduleMapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<CalendarSelectionResource?> Handle(PostCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var calendarSelection = _scheduleMapper.ToCalendarSelectionEntity(request.Resource);
            await _calendarSelectionRepository.Add(calendarSelection);
            await _unitOfWork.CompleteAsync();
            return _scheduleMapper.ToCalendarSelectionResource(calendarSelection);
        }, 
        "creating calendar selection", 
        new { });
    }
}
