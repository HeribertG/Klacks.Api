using Klacks.Api.Resources.Schedules;
using MediatR;

namespace Klacks.Api.Controllers.V1.UserBackend
{
    public class SelectedCalendarsController : InputBaseController<SelectedCalendarResource>
    {
        private readonly ILogger<SelectedCalendarsController> logger;

        public SelectedCalendarsController(IMediator mediator, ILogger<SelectedCalendarsController> logger)
            : base(mediator, logger)
        {
            this.logger = logger;
        }
    }
}
