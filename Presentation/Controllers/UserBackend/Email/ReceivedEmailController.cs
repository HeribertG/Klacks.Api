// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Controller for received emails management (list, read, delete, move, IMAP sync).
/// </summary>
using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Email;

[ApiController]
public class ReceivedEmailController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IImapTestService _imapTestService;

    public ReceivedEmailController(
        IMediator mediator,
        IImapTestService imapTestService)
    {
        _mediator = mediator;
        _imapTestService = imapTestService;
    }

    [HttpGet("GroupTree")]
    public async Task<ActionResult<List<EmailGroupTreeNode>>> GetGroupTree()
    {
        var result = await _mediator.Send(new GetEmailGroupTreeQuery());
        return Ok(result);
    }

    [HttpGet("ByGroup/{groupId:guid}")]
    public async Task<ActionResult<ReceivedEmailListResponse>> GetByGroup(
        Guid groupId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _mediator.Send(new GetEmailsByGroupQuery(groupId, skip, take));
        return Ok(result);
    }

    [HttpGet("ByClient/{clientId:guid}")]
    public async Task<ActionResult<ReceivedEmailListResponse>> GetByClient(
        Guid clientId, [FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _mediator.Send(new GetEmailsByClientQuery(clientId, skip, take));
        return Ok(result);
    }

    [HttpGet("List")]
    public async Task<ActionResult<ReceivedEmailListResponse>> GetList(
        [FromQuery] int skip = 0, [FromQuery] int take = 50,
        [FromQuery] string? folder = null,
        [FromQuery] string? readFilter = null,
        [FromQuery] string? sortDirection = null)
    {
        var result = await _mediator.Send(new GetReceivedEmailsQuery(skip, take, folder, readFilter, sortDirection));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReceivedEmailResource>> GetById(Guid id)
    {
        var result = await _mediator.Send(new GetReceivedEmailQuery(id));
        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpGet("UnreadCount")]
    public async Task<ActionResult<int>> GetUnreadCount()
    {
        var count = await _mediator.Send(new GetUnreadEmailCountQuery());
        return Ok(count);
    }

    [HttpPut("{id:guid}/Read")]
    public async Task<ActionResult<bool>> MarkAsRead(Guid id, [FromQuery] bool isRead = true)
    {
        var result = await _mediator.Send(new MarkEmailAsReadCommand(id, isRead));
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<bool>> Delete(Guid id)
    {
        var result = await _mediator.Send(new DeleteReceivedEmailCommand(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}/Restore")]
    public async Task<ActionResult<bool>> Restore(Guid id)
    {
        var result = await _mediator.Send(new RestoreEmailCommand(id));
        return Ok(result);
    }

    [HttpPut("{id:guid}/MoveToFolder")]
    public async Task<ActionResult<bool>> MoveToFolder(Guid id, [FromQuery] string folder)
    {
        var result = await _mediator.Send(new MoveEmailToFolderCommand(id, folder));
        return Ok(result);
    }

    [HttpDelete("{id:guid}/Permanent")]
    public async Task<ActionResult<bool>> PermanentlyDelete(Guid id)
    {
        var result = await _mediator.Send(new PermanentlyDeleteEmailCommand(id));
        return Ok(result);
    }

    [HttpPost("FetchNow")]
    public async Task<ActionResult<FetchEmailsResult>> FetchNow()
    {
        var result = await _mediator.Send(new FetchEmailsCommand());
        return Ok(result);
    }

    [HttpPost("TestImapConnection")]
    public async Task<ActionResult<ImapTestResult>> TestImapConnection([FromBody] ImapTestRequest request)
    {
        try
        {
            var result = await _imapTestService.TestConnectionAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Ok(new ImapTestResult
            {
                Success = false,
                Message = "An unexpected error occurred during the test.",
                MessageKey = "IMAP_TEST_UNEXPECTED",
                ErrorDetails = ex.Message
            });
        }
    }
}
