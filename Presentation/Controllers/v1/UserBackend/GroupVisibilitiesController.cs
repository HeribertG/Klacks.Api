using Klacks.Api.Commands.GroupVisibilities;
using Klacks.Api.Presentation.Controllers.v1.UserBackend;
using Klacks.Api.Queries;
using Klacks.Api.Queries.GroupVisibilities;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class GroupVisibilitiesController : InputBaseController<GroupResource>
{
    private readonly ILogger<GroupsController> logger;

    public GroupVisibilitiesController(IMediator Mediator, ILogger<GroupsController> logger)
      : base(Mediator, logger)
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
            var groupVisibilities = await Mediator.Send(new GroupVisibilityListQuery(id));
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
            var groupVisibilities = await Mediator.Send(new ListQuery<GroupVisibilityResource>());
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
        await Mediator.Send(new BulkGroupVisibilitiesCommand(bulk));

        return Ok(); 
    }
}
