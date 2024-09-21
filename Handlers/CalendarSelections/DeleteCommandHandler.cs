using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.CalendarSelections;

public class DeleteCommandHandler : IRequestHandler<DeleteCommand<CalendarSelectionResource>, CalendarSelectionResource?>
{
  private readonly ILogger<DeleteCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly ICalendarSelectionRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public DeleteCommandHandler(
                              IMapper mapper,
                              ICalendarSelectionRepository repository,
                              IUnitOfWork unitOfWork,
                              ILogger<DeleteCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<CalendarSelectionResource?> Handle(DeleteCommand<CalendarSelectionResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var calendarSelection = await repository.Delete(request.Id);
      if (calendarSelection == null)
      {
        logger.LogWarning("CalendarSelection with ID {Id} not found for deletion.", request.Id);
        return null;
      }

      await unitOfWork.CompleteAsync();

      logger.LogInformation("CalendarSelection with ID {Id} deleted successfully.", request.Id);

      return mapper.Map<Models.CalendarSelections.CalendarSelection, CalendarSelectionResource>(calendarSelection);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while deleting CalendarSelection with ID {Id}.", request.Id);
      throw;
    }
  }
}
