using Klacks_api.Models.Schedules;
using Klacks_api.Queries;
using Klacks_api.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks_api.Controllers.V1.Backend;

public class ShiftsController : InputBaseController<Shift>
{
  private readonly ILogger<ShiftsController> _logger;

  public ShiftsController(IMediator mediator, ILogger<ShiftsController> logger)
          : base(mediator, logger)
  {
    _logger = logger;
  }

  [HttpGet]
  public async Task<ActionResult<IEnumerable<ShiftResource>>> List() => this.Ok(await this.mediator.Send(new ListQuery<ShiftResource>()));
}
