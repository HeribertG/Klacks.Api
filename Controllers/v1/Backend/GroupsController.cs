using Klacks.Api.Commands.Groups;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using Klacks.Api.Resources.Filter;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Controllers.V1.Backend;

public class GroupsController : InputBaseController<GroupResource>
{
    private readonly ILogger<GroupsController> logger;

    public GroupsController(IMediator mediator, ILogger<GroupsController> logger)
      : base(mediator, logger)
    {
        this.logger = logger;
    }

    [HttpPost("GetSimpleList")]
    public async Task<ActionResult<TruncatedGroupResource>> GetSimpleList([FromBody] GroupFilter filter)
    {
        try
        {
            logger.LogInformation($"Fetching simple group list with filter: {JsonConvert.SerializeObject(filter)}");
            var truncatedGroups = await mediator.Send(new GetTruncatedListQuery(filter));
            logger.LogInformation($"Retrieved {truncatedGroups.Groups.Count} truncated groups.");
            return Ok(truncatedGroups);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching simple group list.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the tree structure for a specific root or all roots if no ID is specified
    /// </summary>
    [HttpGet("tree")]
    public async Task<ActionResult<GroupTreeResource>> GetTree([FromQuery] Guid? rootId = null)
    {
        try
        {
            logger.LogInformation($"Fetching group tree with rootId: {rootId}");
            var tree = await mediator.Send(new GetGroupTreeQuery(rootId));
            logger.LogInformation($"Retrieved tree with {tree.Nodes.Count} nodes.");
            return Ok(tree);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Group tree not found.");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching group tree.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves the path from the root to the specified node
    /// </summary>
    [HttpGet("path/{id}")]
    public async Task<ActionResult<List<GroupResource>>> GetPath(Guid id)
    {
        try
        {
            logger.LogInformation($"Fetching path to node with ID: {id}");
            var path = await mediator.Send(new GetPathToNodeQuery(id));
            logger.LogInformation($"Retrieved path with {path.Count} nodes.");
            return Ok(path);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, $"Path to node with ID {id} not found.");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching path for node ID {id}.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }



    /// <summary>
    /// Moves a group to a new parent
    /// </summary>
    [HttpPost("move/{id}")]
    public async Task<ActionResult<GroupResource>> MoveGroup(Guid id, [FromQuery] Guid newParentId)
    {
        try
        {
            logger.LogInformation($"Moving group with ID: {id} to parent ID: {newParentId}");
            var movedGroup = await mediator.Send(new MoveGroupNodeCommand(id, newParentId));
            logger.LogInformation($"Moved group with ID: {id} to parent ID: {newParentId}");
            return Ok(movedGroup);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, $"Failed to move group with ID {id}.");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, $"Invalid operation when moving group with ID {id}.");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while moving group with ID {id}.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("refresh")]
    public async Task<ActionResult> Refresh()
    {
        await mediator.Send(new RefreshTreeCommand());
        return Ok();
    }

    [HttpGet("roots")]
    public async Task<IEnumerable<GroupResource>> Roots()
    {
        return await mediator.Send(new GetRootsQuery());

    }
}
