using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

public class GroupItemsController(IMediator mediator, ILogger<GroupItemsController> logger, IGroupItemRepository groupItemRepository, DataBaseContext context) : InputBaseController<GroupItemResource>(mediator, logger)
{
    [HttpDelete("remove")]
    public async Task<IActionResult> RemoveByClientAndGroup([FromQuery] Guid clientId, [FromQuery] Guid groupId)
    {
        var groupItem = await groupItemRepository.GetByClientAndGroup(clientId, groupId);
        if (groupItem == null)
        {
            return NotFound();
        }

        groupItemRepository.Remove(groupItem);
        await context.SaveChangesAsync();

        return NoContent();
    }
}
