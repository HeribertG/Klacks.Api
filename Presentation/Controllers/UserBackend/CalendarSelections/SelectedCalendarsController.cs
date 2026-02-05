using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Presentation.Controllers.UserBackend.CalendarSelections;
    public class SelectedCalendarsController : InputBaseController<SelectedCalendarResource>
    {
        private readonly ILogger<SelectedCalendarsController> _logger;

        public SelectedCalendarsController(IMediator Mediator, ILogger<SelectedCalendarsController> logger)
            : base(Mediator, logger)
        {
            this._logger = logger;
        }
    }
