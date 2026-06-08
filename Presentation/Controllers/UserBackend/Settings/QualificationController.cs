// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// CRUD endpoints for the Qualification master entity (admin-only).
/// </summary>

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

[Authorize(Roles = Roles.Admin)]
public class QualificationController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<QualificationController> _logger;

    public QualificationController(IMediator mediator, ILogger<QualificationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("GetQualificationList")]
    public async Task<IEnumerable<Qualification>> GetQualificationList()
    {
        return await _mediator.Send(new ListQuery());
    }

    [HttpPost("AddQualification")]
    public async Task<Qualification> AddQualification([FromBody] Qualification qualification)
    {
        var id = await _mediator.Send(new CreateQualificationCommand(qualification.Name, qualification.Description));
        return await _mediator.Send(new UpdateQualificationCommand(
            id,
            qualification.Name,
            qualification.Description,
            qualification.Emoji,
            qualification.IsTimeLimited,
            qualification.Type,
            qualification.Category,
            qualification.Countries));
    }

    [HttpPut("PutQualification")]
    public async Task<Qualification> PutQualification([FromBody] Qualification qualification)
    {
        return await _mediator.Send(new UpdateQualificationCommand(
            qualification.Id,
            qualification.Name,
            qualification.Description,
            qualification.Emoji,
            qualification.IsTimeLimited,
            qualification.Type,
            qualification.Category,
            qualification.Countries));
    }

    [HttpDelete("DeleteQualification/{id}")]
    public async Task<ActionResult> DeleteQualification(Guid id)
    {
        _logger.LogInformation("DeleteQualification called with ID: {Id}", id);
        await _mediator.Send(new DeleteQualificationCommand(id));
        _logger.LogInformation("DeleteQualification completed for ID: {Id}", id);
        return NoContent();
    }
}
