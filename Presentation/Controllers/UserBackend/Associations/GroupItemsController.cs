// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

/// <summary>
/// Group membership links. Deliberately on SupervisorDeletableController: removing a membership is a
/// supervisor action (spec 2.3 pins RemoveByClientAndGroup that way), and the skills
/// remove_client_from_group (CanEditClients) and remove_shift_from_group (CanEditShifts) delete by id
/// through the self API under the caller's own token, so an Admin-only DELETE would refuse them.
/// </summary>
/// <param name="mediator">Dispatches the group-item commands and queries</param>
public class GroupItemsController(IMediator mediator, ILogger<GroupItemsController> logger) : SupervisorDeletableController<GroupItemResource>(mediator, logger)
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
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
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
