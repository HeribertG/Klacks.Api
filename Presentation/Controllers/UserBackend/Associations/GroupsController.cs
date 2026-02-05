using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

public class GroupsController : InputBaseController<GroupResource>
{
    public GroupsController(IMediator Mediator, ILogger<GroupsController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpPost("GetSimpleList")]
    public async Task<ActionResult<TruncatedGroupResource>> GetSimpleList([FromBody] GroupFilter filter)
    {
        var truncatedGroups = await Mediator.Send(new GetTruncatedListQuery(filter));
        return Ok(truncatedGroups);
    }

    /// <summary>
    /// Retrieves the tree structure for a specific root or all roots if no ID is specified
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<GroupTreeResource>> GetTree([FromQuery] Guid? rootId = null)
    {
        var tree = await Mediator.Send(new GetGroupTreeQuery(rootId));
        return Ok(tree);
    }

    /// <summary>
    /// Retrieves the path from the root to the specified node
    /// </summary>
    [HttpGet("path/{id}")]
    public async Task<ActionResult<List<GroupResource>>> GetPath(Guid id)
    {
        var path = await Mediator.Send(new GetPathToNodeQuery(id));
        return Ok(path);
    }



    /// <summary>
    /// Moves a group to a new parent
    /// </summary>
    [HttpPost("move/{id}")]
    public async Task<ActionResult<GroupResource>> MoveGroup(Guid id, [FromQuery] Guid newParentId)
    {
        var movedGroup = await Mediator.Send(new MoveGroupNodeCommand(id, newParentId));
        return Ok(movedGroup);
    }

    [HttpGet("refresh")]
    public async Task<ActionResult> Refresh()
    {
        await Mediator.Send(new RefreshTreeCommand());
        return Ok();
    }

    [HttpGet("roots")]
    public async Task<IEnumerable<GroupResource>> Roots()
    {
        return await Mediator.Send(new GetRootsQuery());
    }

    /// <summary>
    /// Retrieves all members (GroupItems) for a specific group
    /// </summary>
    [HttpGet("{groupId}/members")]
    public async Task<ActionResult<List<GroupItemResource>>> GetGroupMembers(Guid groupId)
    {
        var members = await Mediator.Send(new GetGroupMembersQuery(groupId));
        return Ok(members);
    }
}
