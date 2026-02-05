using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Application.Mappers.Reports;
using Klacks.Api.Application.Queries.Reports;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Reports;

[ApiController]
[Route("api/backend/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportTemplatesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ReportTemplateMapper _mapper;
    private readonly ILogger<ReportTemplatesController> _logger;

    public ReportTemplatesController(
        IMediator mediator,
        ReportTemplateMapper mapper,
        ILogger<ReportTemplatesController> logger)
    {
        _mediator = mediator;
        _mapper = mapper;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReportTemplateResource>>> GetAll(
        CancellationToken cancellationToken)
    {
        var templates = await _mediator.Send(new GetAllReportTemplatesQuery(), cancellationToken);
        var resources = _mapper.ToResourceList(templates.ToList());
        return Ok(resources);
    }

    [HttpGet("by-type/{type}")]
    public async Task<ActionResult<IEnumerable<ReportTemplateResource>>> GetByType(
        ReportType type,
        CancellationToken cancellationToken)
    {
        var templates = await _mediator.Send(new GetReportTemplatesByTypeQuery(type), cancellationToken);
        var resources = _mapper.ToResourceList(templates.ToList());
        return Ok(resources);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportTemplateResource>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var template = await _mediator.Send(new GetReportTemplateByIdQuery(id), cancellationToken);
        if (template == null)
        {
            return NotFound();
        }
        return Ok(_mapper.ToResource(template));
    }

    [HttpPost]
    public async Task<ActionResult<ReportTemplateResource>> Create(
        [FromBody] ReportTemplateResource resource,
        CancellationToken cancellationToken)
    {
        try
        {
            var template = _mapper.ToEntity(resource);
            var created = await _mediator.Send(new CreateReportTemplateCommand(template), cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, _mapper.ToResource(created));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report template");
            return StatusCode(500, "An error occurred while creating the template");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReportTemplateResource>> Update(
        Guid id,
        [FromBody] ReportTemplateResource resource,
        CancellationToken cancellationToken)
    {
        if (id != resource.Id)
        {
            return BadRequest("ID mismatch");
        }

        try
        {
            var template = _mapper.ToEntity(resource);
            var updated = await _mediator.Send(new UpdateReportTemplateCommand(template), cancellationToken);
            return Ok(_mapper.ToResource(updated));
        }
        catch (ArgumentException ex)
        {
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating report template");
            return StatusCode(500, "An error occurred while updating the template");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _mediator.Send(new DeleteReportTemplateCommand(id), cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report template");
            return StatusCode(500, "An error occurred while deleting the template");
        }
    }
}
