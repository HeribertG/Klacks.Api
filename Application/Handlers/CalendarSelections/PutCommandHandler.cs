using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;
using Klacks.Api.Domain.Exceptions;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    
    public PutCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        }

    public async Task<CalendarSelectionResource?> Handle(PutCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var calendarSelection = _mapper.Map<CalendarSelection>(request.Resource);
            await _calendarSelectionRepository.Update(calendarSelection);
            await _unitOfWork.CompleteAsync();
            
            var updatedCalendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(request.Resource.Id);
            return _mapper.Map<CalendarSelectionResource>(updatedCalendarSelection);
        }, 
        "operation", 
        new { });
    }
}
