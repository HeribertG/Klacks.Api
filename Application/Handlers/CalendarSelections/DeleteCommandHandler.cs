using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly CalendarSelectionApplicationService _calendarSelectionApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCommandHandler> _logger;

    public DeleteCommandHandler(
        CalendarSelectionApplicationService calendarSelectionApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
    {
        _calendarSelectionApplicationService = calendarSelectionApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CalendarSelectionResource?> Handle(DeleteCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var existingCalendarSelection = await _calendarSelectionApplicationService.GetCalendarSelectionByIdAsync(request.Id, cancellationToken);
            if (existingCalendarSelection == null)
            {
                _logger.LogWarning("CalendarSelection with ID {Id} not found for deletion.", request.Id);
                return null;
            }

            await _calendarSelectionApplicationService.DeleteCalendarSelectionAsync(request.Id, cancellationToken);
            await _unitOfWork.CompleteAsync();

            return existingCalendarSelection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting CalendarSelection with ID {Id}.", request.Id);
            throw;
        }
    }
}
