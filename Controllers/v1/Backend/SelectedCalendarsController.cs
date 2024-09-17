using Klacks_api.Resources.Schedules;
using MediatR;

namespace Klacks_api.Controllers.V1.Backend
{
  public class SelectedCalendarsController : InputBaseController<SelectedCalendarResource>
  {
    private readonly ILogger<SelectedCalendarsController> _logger;

    public SelectedCalendarsController(IMediator mediator, ILogger<SelectedCalendarsController> logger)
        : base(mediator, logger)
    {
      _logger = logger;
    }
  }
}
