// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.ClientAvailabilities;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries.ClientAvailabilities;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

public class ClientAvailabilitiesController : BaseController
{
    private readonly IMediator _mediator;

    public ClientAvailabilitiesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientAvailabilityResource>>> List(
        [FromQuery] DateOnly startDate,
        [FromQuery] DateOnly endDate)
    {
        var result = await _mediator.Send(new ListClientAvailabilitiesQuery(startDate, endDate));
        return Ok(result);
    }

    [HttpPost("Bulk")]
    public async Task<ActionResult<int>> BulkUpdate([FromBody] ClientAvailabilityBulkRequest request)
    {
        var result = await _mediator.Send(new BulkUpdateClientAvailabilityCommand(request));
        return Ok(result);
    }
}
