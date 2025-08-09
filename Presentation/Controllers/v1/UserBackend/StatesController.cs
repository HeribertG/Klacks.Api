using Klacks.Api.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class StatesController : InputBaseController<StateResource>
{
    private readonly ILogger<StatesController> logger;

    public StatesController(IMediator mediator, ILogger<StatesController> logger)
      : base(mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StateResource>>> List() => this.Ok(await this.Mediator.Send(new ListQuery<StateResource>()));
}
