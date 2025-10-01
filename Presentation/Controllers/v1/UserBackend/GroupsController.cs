using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.Queries.Groups;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Presentation.DTOs.Filter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class GroupsController : InputBaseController<GroupResource>
{
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IMediator Mediator, ILogger<GroupsController> logger)
      : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpPost("GetSimpleList")]
    public async Task<ActionResult<TruncatedGroupResource>> GetSimpleList([FromBody] GroupFilter filter)
    {
        _logger.LogInformation($"Fetching simple group list with filter: {JsonConvert.SerializeObject(filter)}");
        var truncatedGroups = await Mediator.Send(new GetTruncatedListQuery(filter));
        _logger.LogInformation($"Retrieved {truncatedGroups.Groups.Count} truncated groups.");
        return Ok(truncatedGroups);
    }

    /// <summary>
    /// Retrieves the tree structure for a specific root or all roots if no ID is specified
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<GroupTreeResource>> GetTree([FromQuery] Guid? rootId = null)
    {
        _logger.LogInformation($"Fetching group tree with rootId: {rootId}");
        var tree = await Mediator.Send(new GetGroupTreeQuery(rootId));
        _logger.LogInformation($"Retrieved tree with {tree.Nodes.Count} nodes.");
        return Ok(tree);
    }

    /// <summary>
    /// Retrieves the path from the root to the specified node
    /// </summary>
    [HttpGet("path/{id}")]
    public async Task<ActionResult<List<GroupResource>>> GetPath(Guid id)
    {
        _logger.LogInformation($"Fetching path to node with ID: {id}");
        var path = await Mediator.Send(new GetPathToNodeQuery(id));
        _logger.LogInformation($"Retrieved path with {path.Count} nodes.");
        return Ok(path);
    }



    /// <summary>
    /// Moves a group to a new parent
    /// </summary>
    [HttpPost("move/{id}")]
    public async Task<ActionResult<GroupResource>> MoveGroup(Guid id, [FromQuery] Guid newParentId)
    {
        _logger.LogInformation($"Moving group with ID: {id} to parent ID: {newParentId}");
        var movedGroup = await Mediator.Send(new MoveGroupNodeCommand(id, newParentId));
        _logger.LogInformation($"Moved group with ID: {id} to parent ID: {newParentId}");
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
        _logger.LogInformation($"Fetching members for group with ID: {groupId}");
        var members = await Mediator.Send(new GetGroupMembersQuery(groupId));
        _logger.LogInformation($"Retrieved {members.Count} members for group {groupId}.");
        return Ok(members);
    }
}
