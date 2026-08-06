// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

public class GroupItemsController(IMediator mediator, ILogger<GroupItemsController> logger) : InputBaseController<GroupItemResource>(mediator, logger)
{
    /// <summary>
    /// Creates several group items in one transaction. Exists so a caller that needs the batch to be
    /// all-or-nothing does not have to fake atomicity across N separate requests.
    /// </summary>
    /// <param name="request">The links to create</param>
    [HttpPost("bulk")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<BulkGroupItemResponse>> BulkAdd([FromBody] BulkGroupItemRequest request)
    {
        var response = await Mediator.Send(new BulkAddGroupItemsCommand(request));
        return Ok(response);
    }

    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveByClientAndGroup([FromQuery] Guid clientId, [FromQuery] Guid groupId)
    {
        var found = await Mediator.Send(new RemoveGroupItemByClientAndGroupCommand
        {
            ClientId = clientId,
            GroupId = groupId
        });

        if (!found)
        {
            return NotFound();
        }

        return NoContent();
    }
}
