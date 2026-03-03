// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Assistant;
using System.Security.Claims;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/skills")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class SkillsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SkillsController> _logger;

    public SkillsController(IMediator mediator, ILogger<SkillsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SkillDto>>> GetAllSkills()
    {
        var userPermissions = GetCurrentUserPermissions();
        var skills = await _mediator.Send(new GetSkillsQuery(userPermissions));
        return Ok(skills);
    }

    [HttpGet("{name}")]
    public async Task<ActionResult<SkillDto>> GetSkillByName(string name)
    {
        var skill = await _mediator.Send(new GetSkillByNameQuery(name));
        if (skill == null)
        {
            return NotFound($"Skill '{name}' not found");
        }
        return Ok(skill);
    }

    [HttpPost("execute")]
    public async Task<ActionResult<SkillExecuteResponse>> ExecuteSkill([FromBody] SkillExecuteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.SkillName))
        {
            return BadRequest("Skill name cannot be empty");
        }

        var command = new ExecuteSkillCommand(
            request,
            GetCurrentUserId(),
            GetCurrentTenantId(),
            GetCurrentUserName(),
            GetCurrentUserPermissions());

        var response = await _mediator.Send(command);
        return Ok(response);
    }

    [HttpPost("execute-chain")]
    public async Task<ActionResult<IReadOnlyList<SkillExecuteResponse>>> ExecuteSkillChain([FromBody] SkillChainExecuteRequest request)
    {
        if (request.Invocations == null || request.Invocations.Count == 0)
        {
            return BadRequest("At least one skill invocation is required");
        }

        var command = new ExecuteSkillChainCommand(
            request,
            GetCurrentUserId(),
            GetCurrentTenantId(),
            GetCurrentUserName(),
            GetCurrentUserPermissions());

        var responses = await _mediator.Send(command);
        return Ok(responses);
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<SkillAnalyticsDto>> GetAnalytics([FromQuery] int days = 30)
    {
        var analytics = await _mediator.Send(new GetSkillAnalyticsQuery(days));
        return Ok(analytics);
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return userId;
        }
        return Guid.Empty;
    }

    private string GetCurrentUserName()
    {
        var nameClaim = User.FindFirst(ClaimTypes.Name);
        return nameClaim?.Value ?? "Unknown";
    }

    private Guid GetCurrentTenantId()
    {
        var tenantClaim = User.FindFirst("tenant_id");
        if (tenantClaim != null && Guid.TryParse(tenantClaim.Value, out var tenantId))
        {
            return tenantId;
        }
        return Guid.Empty;
    }

    private List<string> GetCurrentUserPermissions()
    {
        var permissions = new List<string>();

        var roleClaims = User.FindAll(ClaimTypes.Role);
        foreach (var claim in roleClaims)
        {
            permissions.Add(claim.Value);
        }

        if (permissions.Count == 0)
        {
            permissions.AddRange(new[] { "CanViewClients", "CanCreateClients", "CanCreateContracts" });
        }

        return permissions;
    }
}
