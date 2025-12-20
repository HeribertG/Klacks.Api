using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class WorksController : InputBaseController<WorkResource>
{
    public WorksController(IMediator mediator, ILogger<WorksController> logger)
            : base(mediator, logger)
    {
    }
}
