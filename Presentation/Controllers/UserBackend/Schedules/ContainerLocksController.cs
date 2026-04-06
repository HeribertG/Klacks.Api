// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class ContainerLocksController : BaseController
{
    private readonly IMediator _mediator;

    public ContainerLocksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("Acquire")]
    public async Task<ActionResult<ContainerLockResource>> Acquire([FromBody] AcquireContainerLockRequest request)
    {
        var result = await _mediator.Send(new AcquireContainerLockCommand(request.ResourceType, request.ResourceId, request.InstanceId));
        return Ok(result);
    }

    [HttpPost("Heartbeat/{lockId}")]
    public async Task<ActionResult<ContainerLockResource>> Heartbeat(Guid lockId)
    {
        var result = await _mediator.Send(new HeartbeatContainerLockCommand(lockId));
        return Ok(result);
    }

    [HttpDelete("{lockId}")]
    public async Task<ActionResult<bool>> Release(Guid lockId)
    {
        var result = await _mediator.Send(new ReleaseContainerLockCommand(lockId));
        return Ok(result);
    }
}
