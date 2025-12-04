using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class MembershipsController : InputBaseController<MembershipResource>
{
    public MembershipsController(IMediator Mediator, ILogger<MembershipsController> logger)
      : base(Mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MembershipResource>>> GetMembership()
    {
        var memberships = await Mediator.Send(new ListQuery<MembershipResource>());
        return Ok(memberships);
    }
}
