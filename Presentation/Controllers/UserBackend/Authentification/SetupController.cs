// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Accounts;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Authentification;

[ExemptFromAdminSetupGate]
public class SetupController : BaseController
{
    private readonly IAdminSetupGateService _adminSetupGateService;
    private readonly IMediator _mediator;

    public SetupController(IAdminSetupGateService adminSetupGateService, IMediator mediator)
    {
        _adminSetupGateService = adminSetupGateService;
        _mediator = mediator;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("Status")]
    public async Task<ActionResult<SetupStatusResource>> GetStatus()
    {
        var requiresOwnAdmin = await _adminSetupGateService.IsGateActiveAsync();
        return Ok(new SetupStatusResource { RequiresOwnAdmin = requiresOwnAdmin });
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
}
