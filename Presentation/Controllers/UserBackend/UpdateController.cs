// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Admin-only read-side of the auto-update domain: current status, audit history and cancelling a
/// queued operation. Behaviour configuration is read/written through the generic settings endpoints
/// (UPDATE_* keys); execution (trigger/rollback) is added with the manifest reader and updater.
/// </summary>
using System.Security.Claims;
using Klacks.Api.Application.Commands.Update;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Queries.Update;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[Authorize(Roles = Roles.Admin)]
public class UpdateController : BaseController
{
    private const int DefaultHistoryTake = 10;

    private readonly IMediator _mediator;

    public UpdateController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("Status")]
    public async Task<ActionResult<UpdateStatusResult>> GetStatus()
    {
        return Ok(await _mediator.Send(new GetUpdateStatusQuery()));
    }

    [HttpGet("History")]
    public async Task<ActionResult<IReadOnlyList<UpdateHistoryItem>>> GetHistory([FromQuery] int take = DefaultHistoryTake)
    {
        return Ok(await _mediator.Send(new GetUpdateHistoryQuery(take)));
    }

    [HttpGet("Config")]
    public async Task<ActionResult<UpdateConfig>> GetConfig()
    {
        return Ok(await _mediator.Send(new GetUpdateConfigQuery()));
    }

    [BlockedOnPlayground]
    [HttpPut("Config")]
    public async Task<ActionResult<UpdateConfig>> SaveConfig([FromBody] UpdateConfig config)
    {
        return Ok(await _mediator.Send(new SaveUpdateConfigCommand(config)));
    }

    [BlockedOnPlayground]
    [HttpPost("Trigger")]
    public async Task<ActionResult<UpdateTriggerResult>> Trigger()
    {
        var result = await _mediator.Send(new TriggerUpdateCommand(CurrentUserId()));
        return result.Enqueued ? Ok(result) : Conflict(result);
    }

    [BlockedOnPlayground]
    [HttpPost("Rollback")]
    public async Task<ActionResult<UpdateTriggerResult>> Rollback()
    {
        var result = await _mediator.Send(new RequestRollbackCommand(CurrentUserId()));
        return result.Enqueued ? Ok(result) : Conflict(result);
    }

    [BlockedOnPlayground]
    [HttpPost("{id}/Cancel")]
    public async Task<ActionResult> Cancel(Guid id)
    {
        var outcome = await _mediator.Send(new CancelUpdateCommand(id));
        return outcome switch
        {
            UpdateOperationCancellationOutcome.Cancelled => Ok(),
            UpdateOperationCancellationOutcome.NotFound => NotFound(),
            _ => Conflict(),
        };
    }

    [BlockedOnPlayground]
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        var outcome = await _mediator.Send(new DeleteUpdateHistoryCommand(id));
        return outcome switch
        {
            UpdateHistoryDeletionOutcome.Deleted => Ok(),
            UpdateHistoryDeletionOutcome.NotFound => NotFound(),
            _ => Conflict(),
        };
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
}
