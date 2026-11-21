// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Admin endpoints for the local voice (Supertonic 3 in Klacks.Stt): status for the speech settings card, the full
/// licence text the administrator must accept, and the installation, which stores the consent and enqueues a TtsInstall
/// hand-off operation for the out-of-process updater (202; 400 without valid consent; 409 while an operation is active;
/// 403 when the token carries no user id, because the consent must name who accepted).
/// </summary>
/// <param name="mediator">Dispatches the status, licence and install requests</param>
using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Assistant;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/tts/local")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class LocalTtsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LocalTtsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("status")]
    public async Task<ActionResult<LocalTtsStatus>> GetStatus()
    {
        return Ok(await _mediator.Send(new GetLocalTtsStatusQuery()));
    }

    [HttpGet("license")]
    public async Task<ActionResult<LocalTtsLicense>> GetLicense()
    {
        return Ok(await _mediator.Send(new GetLocalTtsLicenseQuery()));
    }

    [BlockedOnPlayground]
    [HttpPost("install")]
    public async Task<ActionResult<LocalTtsInstallResult>> Install([FromBody] LocalTtsInstallRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var result = await _mediator.Send(new InstallLocalTtsCommand(request.AcceptLicense, request.LicenseVersion, userId));
        return result.Outcome switch
        {
            LocalTtsInstallOutcome.Enqueued => Accepted(result),
            LocalTtsInstallOutcome.InvalidConsent => BadRequest(result),
            _ => Conflict(result),
        };
    }
}
