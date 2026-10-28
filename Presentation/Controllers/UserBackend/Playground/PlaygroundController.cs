// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Anonymous status endpoint of the public Playground: tells the browser whether the daily token limit is active
/// and hands out the signed device token when the request carries no valid one. Runs under its own per-IP rate limit
/// (PlaygroundStatus) so tokens cannot be minted in bulk; the policy is separate from the login limit, which the
/// frontend would otherwise exhaust by calling this endpoint on every start, also on customer installations.
/// </summary>
/// <param name="statusService">Builds the status answer</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Playground;
using Klacks.Api.Application.Interfaces.Playground;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Services.Playground;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Playground;

[ApiController]
[AllowAnonymous]
[Route(PlaygroundTokenLimitConstants.StatusRoute)]
public class PlaygroundController : ControllerBase
{
    private readonly IPlaygroundStatusService _statusService;

    public PlaygroundController(IPlaygroundStatusService statusService)
    {
        _statusService = statusService;
    }

    [EnableRateLimiting(RateLimitingPolicies.PlaygroundStatus)]
    [HttpGet(PlaygroundTokenLimitConstants.StatusAction)]
    public ActionResult<PlaygroundStatusResource> GetStatus()
    {
        return Ok(_statusService.GetStatus(PlaygroundVisitorContext.Current));
    }
}
