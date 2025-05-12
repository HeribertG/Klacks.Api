using Klacks.Api.Controllers.V1.Backend;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Queries;
using Klacks.Api.Queries.AssignedGroups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.v1.Backend;

public class AssignedGroupsController(IMediator mediator, ILogger<AssignedGroupsController> logger) : InputBaseController<AssignedGroup>(mediator, logger)
{
    [HttpGet("list")]
    public async Task<IEnumerable<AssignedGroup>> Get(Guid? id)
    {
        try
        {
            logger.LogInformation($"Fetching AssignedGroup List");

            return await mediator.Send(new AssignedGroupListQuery(id));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching AssignedGroup List");
            throw;
        }
    }

}
