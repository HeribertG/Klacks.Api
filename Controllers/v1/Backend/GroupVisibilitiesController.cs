using Klacks.Api.Controllers.V1.Backend;
using Klacks.Api.Queries.GroupVisibilities;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.v1.Backend;

public class GroupVisibilitiesController : InputBaseController<GroupResource>
{
    private readonly ILogger<GroupsController> _logger;

    public GroupVisibilitiesController(IMediator mediator, ILogger<GroupsController> logger)
      : base(mediator, logger)
    {
        _logger = logger;
    }

    [HttpGet("GetSimpleList/{id}")]
    public async Task<ActionResult<IEnumerable<GroupVisibilityResource>>> GetSimpleList(string id)
    {
        try
        {
            _logger.LogInformation($"Fetching simple groupVisibilities list for ID: {id}");
            var groupVisibilities = await mediator.Send(new GroupVisibilityListQuery(id));
            _logger.LogInformation($"Retrieved {groupVisibilities.Count()} simple groupVisibilities list for ID: {id}");
            return Ok(groupVisibilities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while fetching simple groupVisibilities list for ID: {id}");
            throw;
        }
    }
}
