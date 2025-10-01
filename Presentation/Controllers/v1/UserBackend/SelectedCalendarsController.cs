using Klacks.Api.Presentation.DTOs.Schedules;
using MediatR;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;
    public class SelectedCalendarsController : InputBaseController<SelectedCalendarResource>
    {
        private readonly ILogger<SelectedCalendarsController> _logger;

        public SelectedCalendarsController(IMediator Mediator, ILogger<SelectedCalendarsController> logger)
            : base(Mediator, logger)
        {
            this._logger = logger;
        }
    }
