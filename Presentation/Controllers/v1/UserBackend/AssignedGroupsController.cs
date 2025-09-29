using Klacks.Api.Application.Queries.AssignedGroups;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AssignedGroupsController(IMediator mediator, ILogger<AssignedGroupsController> logger) : InputBaseController<GroupResource>(mediator, logger)
{
    [HttpGet("list")]
    public async Task<IEnumerable<GroupResource>> Get(Guid? id)
    {
        logger.LogInformation($"Fetching AssignedGroup List {id}");
        return await Mediator.Send(new AssignedGroupListQuery(id));
    }
}
