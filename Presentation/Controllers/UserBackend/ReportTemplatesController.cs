using Klacks.Api.Domain.Entities.Reports;
using Klacks.Api.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportTemplatesController : ControllerBase
{
    private readonly IReportTemplateRepository _templateRepository;
    private readonly ILogger<ReportTemplatesController> _logger;

    public ReportTemplatesController(
        IReportTemplateRepository templateRepository,
        ILogger<ReportTemplatesController> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReportTemplate>>> GetAll(
        CancellationToken cancellationToken)
    {
        var templates = await _templateRepository.GetAllAsync(cancellationToken);
        return Ok(templates);
    }

    [HttpGet("by-type/{type}")]
    public async Task<ActionResult<IEnumerable<ReportTemplate>>> GetByType(
        ReportType type,
        CancellationToken cancellationToken)
    {
        var templates = await _templateRepository.GetByTypeAsync(type, cancellationToken);
        return Ok(templates);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReportTemplate>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var template = await _templateRepository.GetByIdAsync(id, cancellationToken);
        if (template == null)
        {
            return NotFound();
        }
        return Ok(template);
    }

    [HttpPost]
    public async Task<ActionResult<ReportTemplate>> Create(
        [FromBody] ReportTemplate template,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await _templateRepository.CreateAsync(template, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report template");
            return StatusCode(500, "An error occurred while creating the template");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ReportTemplate>> Update(
        Guid id,
        [FromBody] ReportTemplate template,
        CancellationToken cancellationToken)
    {
        if (id != template.Id)
        {
            return BadRequest("ID mismatch");
        }

        try
        {
            var updated = await _templateRepository.UpdateAsync(template, cancellationToken);
            return Ok(updated);
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
            await _templateRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting report template");
            return StatusCode(500, "An error occurred while deleting the template");
        }
    }
}
