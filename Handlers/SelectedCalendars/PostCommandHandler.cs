using AutoMapper;
using Klacks_api.Commands;
using Klacks_api.Interfaces;
using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Handlers.SelectedCalendars;

public class PostCommandHandler : IRequestHandler<PostCommand<SelectedCalendarResource>, SelectedCalendarResource?>
{
  private readonly ILogger<PostCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly ISelectedCalendarRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PostCommandHandler(
                            IMapper mapper,
                            ISelectedCalendarRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PostCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<SelectedCalendarResource?> Handle(PostCommand<SelectedCalendarResource> request, CancellationToken cancellationToken)
  {
    try
    {
      var selectedCalendar = mapper.Map<SelectedCalendarResource, Models.CalendarSelections.SelectedCalendar>(request.Resource);
      repository.Add(selectedCalendar);

      await unitOfWork.CompleteAsync();

      logger.LogInformation("New SelectedCalendar added successfully. ID: {Id}", selectedCalendar.Id);

      return mapper.Map<Models.CalendarSelections.SelectedCalendar, SelectedCalendarResource>(selectedCalendar);
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while adding a new SelectedCalendar. ID: {Id}", request.Resource.Id);
      throw;
    }
  }
}
