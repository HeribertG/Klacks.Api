using Klacks.Api.Commands.GroupVisibilities;
using Klacks.Api.Controllers.V1.UserBackend;
using Klacks.Api.Queries;
using Klacks.Api.Queries.GroupVisibilities;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.v1.Backend;

public class GroupVisibilitiesController : InputBaseController<GroupResource>
{
    private readonly ILogger<GroupsController> logger;

    public GroupVisibilitiesController(IMediator mediator, ILogger<GroupsController> logger)
      : base(mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet("GetSimpleList/{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GroupVisibilityResource>>> GetPersonalSimpleList(string id)
    {
        try
        {
            logger.LogInformation($"Fetching simple groupVisibilities list for ID: {id}");
            var groupVisibilities = await mediator.Send(new GroupVisibilityListQuery(id));
            logger.LogInformation($"Retrieved {groupVisibilities.Count()} simple groupVisibilities list for ID: {id}");
            return Ok(groupVisibilities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching simple groupVisibilities list for ID: {id}");
            throw;
        }
    }

    [HttpGet("GetSimpleList")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<GroupVisibilityResource>>> GetSimpleList()
    {
        try
        {
            logger.LogInformation($"Fetching simple groupVisibilities");
            var groupVisibilities = await mediator.Send(new ListQuery<GroupVisibilityResource>());
            logger.LogInformation($"Retrieved {groupVisibilities.Count()} simple groupVisibilities list");
            return Ok(groupVisibilities);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching simple groupVisibilities list");
            throw;
        }
    }

    [HttpPost("BulkList")]
    [AllowAnonymous]
    public async Task<ActionResult> BulkList(List<GroupVisibilityResource> bulk)
    {
        logger.LogInformation($"storing Bulk list groupVisibilities");
        await mediator.Send(new BulkGroupVisibilitiesCommand(bulk));

        return Ok(); 
    }
}
