using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class WorksController : InputBaseController<Work>
{
    public WorksController(IMediator mediator, ILogger<WorksController> logger)
            : base(mediator, logger)
    {
    }
}
