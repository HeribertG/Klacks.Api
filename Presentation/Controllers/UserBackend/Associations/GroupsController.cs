// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Associations;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Domain.DTOs.Filter;
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

    /// <summary>
    /// Soft-deletes a group together with its children in one transaction. Exists because the plain
    /// DELETE removes one row: a caller wanting the subtree gone cannot roll back the children that
    /// already succeeded, and a half-deleted tree is worse than none.
    /// </summary>
    /// <param name="id">The group whose subtree is removed</param>
    [HttpDelete("{id}/subtree")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<DeleteGroupSubtreeResponse>> DeleteSubtree(Guid id)
    {
        var response = await Mediator.Send(new DeleteGroupSubtreeCommand(id));
        return Ok(response);
    }

    /// <summary>
    /// Stores a group's coordinates. Separate from the generic PUT because GroupResource carries no
    /// latitude or longitude, and the write also marks the group as geocoded.
    /// </summary>
    /// <param name="id">The group being located</param>
    /// <param name="location">The coordinates to store</param>
    [HttpPut("{id}/location")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<IActionResult> SetLocation(Guid id, [FromBody] GroupLocationResource location)
    {
        await Mediator.Send(new SetGroupLocationCommand(id, location.Latitude, location.Longitude));
        return NoContent();
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
