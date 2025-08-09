using Klacks.Api.Presentation.Controllers.v1.UserBackend;
using Klacks.Api.Models.Staffs;
using Klacks.Api.Queries.AssignedGroups;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class AssignedGroupsController(IMediator mediator, ILogger<AssignedGroupsController> logger) : InputBaseController<AssignedGroup>(mediator, logger)
{
    [HttpGet("list")]
    public async Task<IEnumerable<AssignedGroup>> Get(Guid? id)
    {
        try
        {
            logger.LogInformation($"Fetching AssignedGroup List {id}");

            return await Mediator.Send(new AssignedGroupListQuery(id));

        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error occurred while fetching AssignedGroup List {id}");
            throw;
        }
    }

}
