// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Controller for email folder management (create, list, delete). Listing the folders belongs to reading the
/// inbox (Admin and Authorised); creating and deleting a folder changes the mailbox layout and is Admin-only,
/// as the inbox UI already offers it to admins only.
/// </summary>
using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Email;

[ApiController]
[Route("api/backend/ReceivedEmail")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
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
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<EmailFolderResource>> CreateFolder([FromBody] CreateEmailFolderCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("Folders/{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<bool>> DeleteFolder(Guid id)
    {
        var result = await _mediator.Send(new DeleteEmailFolderCommand(id));
        return Ok(result);
    }
}
