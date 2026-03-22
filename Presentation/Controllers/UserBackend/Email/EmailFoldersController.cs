// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for email folder management (create, list, delete).
/// </summary>
using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Email;

[ApiController]
[Route("api/backend/ReceivedEmail")]
public class EmailFoldersController : BaseController
{
    private readonly IMediator _mediator;

    public EmailFoldersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("Folders")]
    public async Task<ActionResult<List<EmailFolderResource>>> GetFolders()
    {
        var result = await _mediator.Send(new GetEmailFoldersQuery());
        return Ok(result);
    }

    [HttpPost("Folders")]
    public async Task<ActionResult<EmailFolderResource>> CreateFolder([FromBody] CreateEmailFolderCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("Folders/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteFolder(Guid id)
    {
        var result = await _mediator.Send(new DeleteEmailFolderCommand(id));
        return Ok(result);
    }
}
