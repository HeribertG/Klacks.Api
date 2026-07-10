// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Admin endpoints for the self-hosted Whisper STT plugin: status for the settings card, and
/// install/uninstall which enqueue hand-off operations executed by the out-of-process updater.
/// </summary>
using System.Security.Claims;
using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.DTOs.Update;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/stt/whisper-plugin")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
public class WhisperPluginController : ControllerBase
{
    private readonly IMediator _mediator;

    public WhisperPluginController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("status")]
    public async Task<ActionResult<WhisperPluginStatus>> GetStatus()
    {
        return Ok(await _mediator.Send(new GetWhisperPluginStatusQuery()));
    }

    [HttpPost("install")]
    public async Task<ActionResult<UpdateTriggerResult>> Install([FromBody] Presentation.DTOs.Assistant.WhisperPluginInstallRequest request)
    {
        var result = await _mediator.Send(new InstallWhisperPluginCommand(request.ModelAlias, CurrentUserId()));
        return result.Enqueued ? Ok(result) : Conflict(result);
    }

    [HttpPost("uninstall")]
    public async Task<ActionResult<UpdateTriggerResult>> Uninstall()
    {
        var result = await _mediator.Send(new UninstallWhisperPluginCommand(CurrentUserId()));
        return result.Enqueued ? Ok(result) : Conflict(result);
    }

    private string CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
}
