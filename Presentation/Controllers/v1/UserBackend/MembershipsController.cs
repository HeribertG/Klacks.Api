using Klacks.Api.Application.Queries;
using Klacks.Api.Presentation.DTOs.Associations;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class MembershipsController : InputBaseController<MembershipResource>
{
    private readonly ILogger<MembershipsController> _logger;

    public MembershipsController(IMediator Mediator, ILogger<MembershipsController> logger)
      : base(Mediator, logger)
    {
        this._logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MembershipResource>>> GetMembership()
    {
        _logger.LogInformation("Fetching all memberships.");
        var memberships = await Mediator.Send(new ListQuery<MembershipResource>());
        _logger.LogInformation($"Retrieved {memberships.Count()} memberships.");
        return Ok(memberships);
    }
}
