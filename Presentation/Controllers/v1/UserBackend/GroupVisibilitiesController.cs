using Klacks.Api.Application.Commands.GroupVisibilities;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.GroupVisibilities;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class GroupVisibilitiesController : InputBaseController<GroupResource>
{
    private readonly ILogger<GroupsController> _logger;

    public GroupVisibilitiesController(IMediator Mediator, ILogger<GroupsController> logger)
      : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet("GetSimpleList/{id}")]
    public async Task<ActionResult<IEnumerable<GroupVisibilityResource>>> GetPersonalSimpleList(string id)
    {
        _logger.LogInformation($"Fetching simple groupVisibilities list for ID: {id}");
        var groupVisibilities = await Mediator.Send(new GroupVisibilityListQuery(id));
        _logger.LogInformation($"Retrieved {groupVisibilities.Count()} simple groupVisibilities list for ID: {id}");
        return Ok(groupVisibilities);
    }

    [HttpGet("GetSimpleList")]
    public async Task<ActionResult<IEnumerable<GroupVisibilityResource>>> GetSimpleList()
    {
        _logger.LogInformation($"Fetching simple groupVisibilities");
        var groupVisibilities = await Mediator.Send(new ListQuery<GroupVisibilityResource>());
        _logger.LogInformation($"Retrieved {groupVisibilities.Count()} simple groupVisibilities list");
        return Ok(groupVisibilities);
    }

    [HttpPost("BulkList")]
    public async Task<ActionResult> BulkList(List<GroupVisibilityResource> bulk)
    {
        _logger.LogInformation($"storing Bulk list groupVisibilities");
        await Mediator.Send(new BulkGroupVisibilitiesCommand(bulk));

        return Ok();
    }
}
