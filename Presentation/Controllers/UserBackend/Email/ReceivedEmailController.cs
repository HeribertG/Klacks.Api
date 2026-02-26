// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
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

    public ReceivedEmailController(IMediator mediator, IImapTestService imapTestService)
    {
        _mediator = mediator;
        _imapTestService = imapTestService;
    }

    [HttpGet("List")]
    public async Task<ActionResult<ReceivedEmailListResponse>> GetList([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var result = await _mediator.Send(new GetReceivedEmailsQuery(skip, take));
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
}
