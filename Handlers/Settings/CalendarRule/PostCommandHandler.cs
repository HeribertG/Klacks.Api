using AutoMapper;
using Klacks_api.Commands.Settings.CalendarRules;
using Klacks_api.Interfaces;
using MediatR;
using Klacks_api.Resources.Settings;

namespace Klacks_api.Handlers.Settings.CalendarRules;

public class PostCommandHandler : IRequestHandler<PostCommand, Models.Settings.CalendarRule?>
{
  private readonly ILogger<PostCommandHandler> logger;
  private readonly IMapper mapper;
  private readonly ISettingsRepository repository;
  private readonly IUnitOfWork unitOfWork;

  public PostCommandHandler(
                            IMapper mapper,
                            ISettingsRepository repository,
                            IUnitOfWork unitOfWork,
                            ILogger<PostCommandHandler> logger)
  {
    this.mapper = mapper;
    this.repository = repository;
    this.unitOfWork = unitOfWork;
    this.logger = logger;
  }

  public async Task<Models.Settings.CalendarRule?> Handle(PostCommand request, CancellationToken cancellationToken)
  {
    try
    {
      var calendarRule = mapper.Map<CalendarRuleResource, Models.Settings.CalendarRule>(request.model);
      repository.AddCalendarRule(calendarRule);

      await unitOfWork.CompleteAsync();

      logger.LogInformation("New CalendarRule added successfully. ID: {CalendarRuleId}", calendarRule.Id);

      return calendarRule;
    }
    catch (Exception ex)
    {
      logger.LogError(ex, "Error occurred while adding a new CalendarRule. ID: {CalendarRuleId}", request.model.Id);
      throw;
    }
  }
}
