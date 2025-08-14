using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class PutCommandHandler : IRequestHandler<PutCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CalendarSelectionResource?> Handle(PutCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var calendarSelection = _mapper.Map<CalendarSelection>(request.Resource);
            await _calendarSelectionRepository.Update(calendarSelection);
            await _unitOfWork.CompleteAsync();
            
            var updatedCalendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(request.Resource.Id);
            return _mapper.Map<CalendarSelectionResource>(updatedCalendarSelection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating CalendarSelection. ID: {Id}", request.Resource.Id);
            throw;
        }
    }
}
