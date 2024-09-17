using Klacks_api.Models.Schedules;
using Klacks_api.Queries.Works;
using Klacks_api.Resources.Filter;
using Klacks_api.Resources.Schedules;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks_api.Controllers.V1.Backend;

public class WorksController : InputBaseController<Work>
{
  private readonly ILogger<WorksController> _logger;

  public WorksController(IMediator mediator, ILogger<WorksController> logger)
          : base(mediator, logger)
  {
    _logger = logger;
  }

  /// <summary>
  /// List of all Clients and all their Works in the current Year and Month.
  /// </summary>
  /// <param name="filter">Current Filter.</param>
  [HttpPost("GetClientList")]
  public async Task<ActionResult<IEnumerable<ClientWorkResource>>> GetClientList([FromBody] WorkFilter filter)
  {
    return Ok(await mediator.Send(new ListQuery(filter)));
  }
}
