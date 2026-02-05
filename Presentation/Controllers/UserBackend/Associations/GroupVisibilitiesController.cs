using Klacks.Api.Application.Commands.GroupVisibilities;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.GroupVisibilities;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Associations;

public class GroupVisibilitiesController : InputBaseController<GroupResource>
{
    public GroupVisibilitiesController(IMediator Mediator, ILogger<GroupsController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpGet("GetSimpleList/{id}")]
    public async Task<ActionResult<IEnumerable<GroupVisibilityResource>>> GetPersonalSimpleList(string id)
    {
        var groupVisibilities = await Mediator.Send(new GroupVisibilityListQuery(id));
        return Ok(groupVisibilities);
    }

    [HttpGet("GetSimpleList")]
    public async Task<ActionResult<IEnumerable<GroupVisibilityResource>>> GetSimpleList()
    {
        var groupVisibilities = await Mediator.Send(new ListQuery<GroupVisibilityResource>());
        return Ok(groupVisibilities);
    }

    [HttpPost("BulkList")]
    public async Task<ActionResult> BulkList(List<GroupVisibilityResource> bulk)
    {
        await Mediator.Send(new BulkGroupVisibilitiesCommand(bulk));
        return Ok();
    }
}
