// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Own-admin setup of a freshly deployed instance. Two ways lead to it: the seeded admin signed in (Status,
/// CompleteOwnAdmin) and the anonymous first start (FirstStartStatus, CompleteFirstStart), which only works while the
/// installation can still be claimed and otherwise answers "not available" without saying why. A first-start request
/// that does not come straight from the machine itself (DirectLoopbackRequest) has to carry the installer's setup code.
/// </summary>
/// <param name="mediator">Dispatches the setup status query and the completion command</param>

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Authentication;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Authentification;

[ExemptFromAdminSetupGate]
public class SetupController : BaseController
{
    private readonly IMediator _mediator;

    public SetupController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("Status")]
    public async Task<ActionResult<SetupStatusResource>> GetStatus()
    {
        return Ok(await _mediator.Send(new GetSetupStatusQuery()));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    [HttpPost("CompleteOwnAdmin")]
    public async Task<ActionResult> CompleteOwnAdmin([FromBody] RegistrationResource model)
    {
        var callerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (callerId != SystemAccounts.SeedAdminUserId)
        {
            return StatusCode(StatusCodes.Status403Forbidden);
        }

        await _mediator.Send(new CompleteOwnAdminSetupCommand(model));
        return Ok();
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.SetupStatus)]
    [HttpGet("FirstStartStatus")]
    public async Task<ActionResult<SetupStatusResource>> GetFirstStartStatus()
    {
        return Ok(await _mediator.Send(new GetSetupStatusQuery(
            IsFirstStart: true,
            IsLocalRequest: DirectLoopbackRequest.IsDirectLoopback(HttpContext))));
    }

    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingPolicies.Login)]
    [HttpPost("CompleteFirstStart")]
    public async Task<ActionResult> CompleteFirstStart([FromBody] FirstStartRegistrationResource model)
    {
        await _mediator.Send(new CompleteOwnAdminSetupCommand(
            model,
            IsFirstStart: true,
            IsLocalRequest: DirectLoopbackRequest.IsDirectLoopback(HttpContext),
            SetupCode: model.SetupCode));
        return Ok();
    }
}
