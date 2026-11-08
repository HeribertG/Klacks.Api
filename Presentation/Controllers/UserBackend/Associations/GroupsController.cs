// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

/// <summary>
/// The group structure: create, edit, move, locate and delete groups. Every structural write is Admin-only, because the
/// group structure is a setting (owner decision 2026-11-07: roles are work areas, Admin holds every setting). Putting an
/// employee into a group is employee maintenance and stays with the supervisor through the client aggregate and
/// GroupItemsController; PUT here also carries the member list of the group page, which is therefore an Admin page. The
/// Post and Put overrides only add the Admin restriction: an [Authorize] on an override is AND-combined with the base
/// Admin/Authorised attribute, so the effective rule is Admin. Reads stay open.
/// </summary>
public class GroupsController : InputBaseController<GroupResource>
{
    public GroupsController(IMediator Mediator, ILogger<GroupsController> logger)
      : base(Mediator, logger)
    {
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<GroupResource>> Post([FromBody] GroupResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<GroupResource>> Put([FromBody] GroupResource resource) =>
        base.Put(resource);

    /// <summary>
    /// Soft-deletes a group together with its children in one transaction. Exists because the plain
    /// DELETE removes one row: a caller wanting the subtree gone cannot roll back the children that
    /// already succeeded, and a half-deleted tree is worse than none.
    /// </summary>
    /// <param name="id">The group whose subtree is removed</param>
    [HttpDelete("{id}/subtree")]
    [Authorize(Roles = Roles.Admin)]
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
    [Authorize(Roles = Roles.Admin)]
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
    /// Retrieves the tree structure for a specific root or all roots if no ID is specified. The tree feeds every
    /// group picker, so it is limited to the groups the caller can see (admins see all; owner decision 2026-10-01):
    /// a picker must not offer a group whose writes would be refused.
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<GroupTreeResource>> GetTree([FromQuery] Guid? rootId = null)
    {
        var tree = await Mediator.Send(new GetGroupTreeQuery(rootId, ApplyVisibilityScope: true));
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
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<GroupResource>> MoveGroup(Guid id, [FromQuery] Guid newParentId)
    {
        var movedGroup = await Mediator.Send(new MoveGroupNodeCommand(id, newParentId));
        return Ok(movedGroup);
    }

    [HttpPost("refresh")]
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
