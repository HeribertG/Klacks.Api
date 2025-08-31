using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public DeleteCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _mapper = mapper;
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

            return _mapper.Map<CalendarSelectionResource>(existingCalendarSelection);
        }, 
        "deleting calendar selection", 
        new { CalendarSelectionId = request.Id });
    }
}
