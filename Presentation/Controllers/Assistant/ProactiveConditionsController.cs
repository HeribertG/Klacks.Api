// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only REST access to the proactive condition ledger for a planner's own scope. Today it serves a
/// single question the service grid asks: of the entities currently on screen, which ones did Klacksy's
/// remediation already handle. Its own controller rather than an endpoint on ProactiveMessagesController,
/// which owns the per-user message inbox and nothing else, and not on ProactiveGovernanceController,
/// which is Roles.Admin and would lock out exactly the Authorised planners this serves.
///
/// POST rather than GET although it reads: the request carries one entity id per visible grid cell, which
/// overruns the URL length limit long before it overruns ConditionAttributionDefaults.MaxEntityIdsPerRequest.
/// A caller who is not a planner receives an empty list, not a Forbidden - the handler decides that, and
/// hiding rows is what every other scoped ledger read does rather than confirming that they exist.
/// </summary>
/// <param name="mediator">Dispatches the attribution query.</param>

using System.Security.Claims;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/proactive-conditions")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProactiveConditionsController : ControllerBase
{
    private const string TooManyEntityIdsMessage = "Too many entity ids in one request.";

    private readonly IMediator _mediator;

    public ProactiveConditionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("attributions")]
    public async Task<ActionResult<IReadOnlyList<ConditionAttributionDto>>> GetAttributions(
        [FromBody] GetConditionAttributionsRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EntityIds.Count > ConditionAttributionDefaults.MaxEntityIdsPerRequest)
        {
            return BadRequest(TooManyEntityIdsMessage);
        }

        var attributions = await _mediator.Send(new GetConditionAttributionsQuery
        {
            UserId = GetCurrentUserId(),
            EntityIds = request.EntityIds
        }, cancellationToken);

        return Ok(attributions);
    }

    private string GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
