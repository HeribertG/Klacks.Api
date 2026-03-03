// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

public class GroupItemsController(IMediator mediator, ILogger<GroupItemsController> logger) : InputBaseController<GroupItemResource>(mediator, logger)
{
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
