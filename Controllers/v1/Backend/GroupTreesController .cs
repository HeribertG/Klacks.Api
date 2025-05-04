using Klacks.Api.Commands.Groups;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.Backend
{
    public class GroupTreesController : BaseController
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GroupTreesController> _logger;

        public GroupTreesController(IMediator mediator, ILogger<GroupTreesController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves the tree structure for a specific root or all roots if no ID is specified
        /// </summary>
        [HttpGet("tree")]
        [AllowAnonymous]
        public async Task<ActionResult<GroupTreeResource>> GetTree([FromQuery] Guid? rootId = null)
        {
            try
            {
                _logger.LogInformation($"Fetching group tree with rootId: {rootId}");
                var tree = await _mediator.Send(new GetGroupTreeQuery(rootId));
                _logger.LogInformation($"Retrieved tree with {tree.Nodes.Count} nodes.");
                return Ok(tree);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Group tree not found.");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching group tree.");
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        /// <summary>
        /// Holt den Pfad von der Wurzel bis zum angegebenen Knoten
        /// </summary>
        [HttpGet("path/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<List<GroupTreeNodeResource>>> GetPath(Guid id)
        {
            try
            {
                _logger.LogInformation($"Fetching path to node with ID: {id}");
                var path = await _mediator.Send(new GetPathToNodeQuery(id));
                _logger.LogInformation($"Retrieved path with {path.Count} nodes.");
                return Ok(path);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Path to node with ID {id} not found.");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching path for node ID {id}.");
                return StatusCode(500, $"Interner Server-Fehler: {ex.Message}");
            }
        }

       

        /// <summary>
        /// Verschiebt eine Gruppe zu einem neuen Elternteil
        /// </summary>
        [HttpPost("move/{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<GroupTreeNodeResource>> MoveGroup(Guid id, [FromQuery] Guid newParentId)
        {
            try
            {
                _logger.LogInformation($"Moving group with ID: {id} to parent ID: {newParentId}");
                var movedGroup = await _mediator.Send(new MoveGroupNodeCommand(id, newParentId));
                _logger.LogInformation($"Moved group with ID: {id} to parent ID: {newParentId}");
                return Ok(movedGroup);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Failed to move group with ID {id}.");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid operation when moving group with ID {id}.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while moving group with ID {id}.");
                return StatusCode(500, $"Interner Server-Fehler: {ex.Message}");
            }
        }
    }
}