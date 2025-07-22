using Klacks.Api.Queries;
using Klacks.Api.Resources.Associations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.V1.UserBackend;

public class MembershipsController : InputBaseController<MembershipResource>
{
    private readonly ILogger<MembershipsController> logger;

    public MembershipsController(IMediator Mediator, ILogger<MembershipsController> logger)
      : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MembershipResource>>> GetMembership()
    {
        try
        {
            logger.LogInformation("Fetching all memberships.");
            var memberships = await Mediator.Send(new ListQuery<MembershipResource>());
            logger.LogInformation($"Retrieved {memberships.Count()} memberships.");
            return Ok(memberships);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching memberships.");
            throw;
        }
    }
}
