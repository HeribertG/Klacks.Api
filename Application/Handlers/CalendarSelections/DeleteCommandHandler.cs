using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;
    private readonly IMapper _mapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        ICalendarSelectionRepository calendarSelectionRepository,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
        _mapper = mapper;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CalendarSelectionResource?> Handle(DeleteCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingCalendarSelection = await _calendarSelectionRepository.GetWithSelectedCalendars(request.Id);
            if (existingCalendarSelection == null)
            {
                _logger.LogWarning("CalendarSelection with ID {Id} not found for deletion.", request.Id);
                return null;
            }

            await _calendarSelectionRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return _mapper.Map<CalendarSelectionResource>(existingCalendarSelection);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting CalendarSelection with ID {Id}.", request.Id);
            throw;
        }
    }
}
