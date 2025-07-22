using Klacks.Api.Queries;
using Klacks.Api.Resources.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.UserBackend;

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
