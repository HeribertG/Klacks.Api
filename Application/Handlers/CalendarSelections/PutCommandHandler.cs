using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class PutCommandHandler : IRequestHandler<PutCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly CalendarSelectionApplicationService _calendarSelectionApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PutCommandHandler> _logger;

    public PutCommandHandler(
        CalendarSelectionApplicationService calendarSelectionApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
    {
        _calendarSelectionApplicationService = calendarSelectionApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CalendarSelectionResource?> Handle(PutCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        try
        {
            await _calendarSelectionApplicationService.UpdateCalendarSelectionDirectAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            
            var updatedCalendarSelection = await _calendarSelectionApplicationService.GetCalendarSelectionByIdAsync(request.Resource.Id, cancellationToken);
            return updatedCalendarSelection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating CalendarSelection. ID: {Id}", request.Resource.Id);
            throw;
        }
    }
}
