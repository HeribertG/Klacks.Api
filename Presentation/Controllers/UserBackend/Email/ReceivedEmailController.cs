// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Email;

[ApiController]
public class ReceivedEmailController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IImapTestService _imapTestService;
    private readonly IImapEmailService _imapEmailService;
    private readonly IUnitOfWork _unitOfWork;

    public ReceivedEmailController(
        IMediator mediator,
        IImapTestService imapTestService,
        IImapEmailService imapEmailService,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _imapTestService = imapTestService;
        _imapEmailService = imapEmailService;
        _unitOfWork = unitOfWork;
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

    [HttpPost("FetchNow")]
    public async Task<ActionResult> FetchNow()
    {
        try
        {
            var newEmails = await _imapEmailService.FetchNewEmailsAsync();
            if (newEmails.Count > 0)
            {
                await _unitOfWork.CompleteAsync();
            }

            return Ok(new { success = true, fetchedCount = newEmails.Count });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, error = ex.Message, innerError = ex.InnerException?.Message });
        }
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
                ErrorDetails = ex.Message
            });
        }
    }

    [HttpGet("SpamRules")]
    public async Task<ActionResult<List<SpamRuleResource>>> GetSpamRules()
    {
        var result = await _mediator.Send(new GetSpamRulesQuery());
        return Ok(result);
    }

    [HttpPost("SpamRules")]
    public async Task<ActionResult<SpamRuleResource>> CreateSpamRule([FromBody] CreateSpamRuleCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpPut("SpamRules/{id:guid}")]
    public async Task<ActionResult<SpamRuleResource>> UpdateSpamRule(Guid id, [FromBody] UpdateSpamRuleCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("SpamRules/{id:guid}")]
    public async Task<ActionResult<bool>> DeleteSpamRule(Guid id)
    {
        var result = await _mediator.Send(new DeleteSpamRuleCommand(id));
        return Ok(result);
    }
}
