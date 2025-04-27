using AutoMapper;
using Klacks.Api.Commands;
using Klacks.Api.Interfaces;
using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Handlers.SelectedCalendars;

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
      await repository.Add(selectedCalendar);

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
