using Klacks.Api.Commands.Groups;
using Klacks.Api.Queries.Groups;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Controllers.V1.Backend
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class GroupTreeController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<GroupTreeController> _logger;

        public GroupTreeController(IMediator mediator, ILogger<GroupTreeController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Holt die Baumstruktur für eine bestimmte Wurzel oder alle Wurzeln, wenn keine ID angegeben ist
        /// </summary>
        [HttpGet("tree")]
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
                return StatusCode(500, $"Interner Server-Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Holt Details zu einer spezifischen Gruppe
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<GroupTreeNodeResource>> GetGroupDetails(Guid id)
        {
            try
            {
                _logger.LogInformation($"Fetching group details for ID: {id}");
                var group = await _mediator.Send(new GetGroupNodeDetailsQuery(id));
                return Ok(group);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Group with ID {id} not found.");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching group details for ID {id}.");
                return StatusCode(500, $"Interner Server-Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Holt den Pfad von der Wurzel bis zum angegebenen Knoten
        /// </summary>
        [HttpGet("path/{id}")]
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
        /// Erstellt eine neue Gruppe (als Wurzel oder als Kind einer bestehenden Gruppe)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<GroupTreeNodeResource>> CreateGroup([FromQuery] Guid? parentId, [FromBody] GroupCreateResource groupResource)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation($"Creating new group node with parent ID: {parentId}, Group: {JsonConvert.SerializeObject(groupResource)}");
                var createdGroup = await _mediator.Send(new CreateGroupNodeCommand(parentId, groupResource));
                _logger.LogInformation($"Created new group with ID: {createdGroup.Id}");
                return CreatedAtAction(nameof(GetGroupDetails), new { id = createdGroup.Id }, createdGroup);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Failed to create group node.");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating new group node.");
                return StatusCode(500, $"Interner Server-Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Aktualisiert eine bestehende Gruppe
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<GroupTreeNodeResource>> UpdateGroup(Guid id, [FromBody] GroupUpdateResource groupResource)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation($"Updating group with ID: {id}, Group: {JsonConvert.SerializeObject(groupResource)}");
                var updatedGroup = await _mediator.Send(new UpdateGroupNodeCommand(id, groupResource));
                _logger.LogInformation($"Updated group with ID: {updatedGroup.Id}");
                return Ok(updatedGroup);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Group with ID {id} not found for update.");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid operation for group with ID {id}.");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while updating group with ID {id}.");
                return StatusCode(500, $"Interner Server-Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Löscht eine Gruppe und all ihre Untergruppen
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteGroup(Guid id)
        {
            try
            {
                _logger.LogInformation($"Deleting group with ID: {id}");
                var result = await _mediator.Send(new DeleteGroupNodeCommand(id));
                _logger.LogInformation($"Group with ID {id} deleted successfully.");
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Group with ID {id} not found for deletion.");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting group with ID {id}.");
                return StatusCode(500, $"Interner Server-Fehler: {ex.Message}");
            }
        }

        /// <summary>
        /// Verschiebt eine Gruppe zu einem neuen Elternteil
        /// </summary>
        [HttpPost("move/{id}")]
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