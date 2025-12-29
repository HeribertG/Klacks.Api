using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class StatesController : InputBaseController<StateResource>
{
    private readonly ILogger<StatesController> _logger;

    public StatesController(IMediator mediator, ILogger<StatesController> logger)
      : base(mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StateResource>>> List() => this.Ok(await this.Mediator.Send(new ListQuery<StateResource>()));
}
