using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Presentation.DTOs.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

[ApiController]
public class DashboardController : BaseController
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ClientLocations")]
    public async Task<ActionResult<IEnumerable<ClientLocationResource>>> GetClientLocations()
    {
        var locations = await _mediator.Send(new GetClientLocationsQuery());
        return Ok(locations);
    }
}
