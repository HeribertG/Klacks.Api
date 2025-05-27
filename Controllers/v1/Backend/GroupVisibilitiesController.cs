using Klacks.Api.Controllers.V1.Backend;
using Klacks.Api.Queries.Groups;
using MediatR;
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

    [HttpGet("visibility/{id}")]
    public async Task<IEnumerable<GroupResource>> GroupVisibility(Guid id)
    {
        return await mediator.Send(new GetGroupVisibilityListQuery(id));

    }
}
