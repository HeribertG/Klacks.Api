using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.CalendarSelections;

public class PostCommandHandler : IRequestHandler<PostCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
    private readonly CalendarSelectionApplicationService _calendarSelectionApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PostCommandHandler> _logger;

    public PostCommandHandler(
        CalendarSelectionApplicationService calendarSelectionApplicationService,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
    {
        _calendarSelectionApplicationService = calendarSelectionApplicationService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CalendarSelectionResource?> Handle(PostCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _calendarSelectionApplicationService.CreateCalendarSelectionAsync(request.Resource, cancellationToken);
            await _unitOfWork.CompleteAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while adding a new CalendarSelection. ID: {Id}", request.Resource.Id);
            throw;
        }
    }
}
