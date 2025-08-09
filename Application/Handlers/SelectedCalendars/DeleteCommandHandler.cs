using AutoMapper;
using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Application.Handlers.SelectedCalendars;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
    private readonly ILogger<DeleteCommandHandler> logger;
    private readonly IMapper mapper;
    private readonly ISelectedCalendarRepository repository;
    private readonly IUnitOfWork unitOfWork;

    public DeleteCommandHandler(
                                IMapper mapper,
                                ISelectedCalendarRepository repository,
                                IUnitOfWork unitOfWork,
                                ILogger<DeleteCommandHandler> logger)
    {
        this.mapper = mapper;
        this.repository = repository;
        this.unitOfWork = unitOfWork;
        this.logger = logger;
    }

    public async Task<SelectedCalendarResource?> Handle(DeleteCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
    {
        try
        {
            var selectedCalendar = await repository.Delete(request.Id);
            if (selectedCalendar == null)
            {
                logger.LogWarning("SelectedCalendar with ID {Id} not found for deletion.", request.Id);
                return null;
            }

            await unitOfWork.CompleteAsync();

            logger.LogInformation("SelectedCalendar with ID {Id} deleted successfully.", request.Id);

            return mapper.Map<Models.CalendarSelections.SelectedCalendar, SelectedCalendarResource>(selectedCalendar);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while deleting SelectedCalendar with ID {Id}.", request.Id);
            throw;
        }
    }
}
