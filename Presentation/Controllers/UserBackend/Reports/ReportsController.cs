using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Reports;

[ApiController]
[Route("api/backend/[controller]")]
[Authorize(Roles = "Admin")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(IMediator mediator, ILogger<ReportsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("schedule/{clientId:guid}")]
    public async Task<IActionResult> GenerateScheduleReport(
        Guid clientId,
        [FromBody] GenerateScheduleReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Generating schedule report for client {ClientId} from {FromDate} to {ToDate}",
                clientId, request.FromDate, request.ToDate);

            var command = new GenerateScheduleReportCommand(
                clientId,
                request.FromDate,
                request.ToDate,
                request.TemplateId);

            var pdf = await _mediator.Send(command, cancellationToken);

            var fileName = $"Schedule_{clientId}_{request.FromDate:yyyyMMdd}_{request.ToDate:yyyyMMdd}.pdf";

            return File(pdf, "application/pdf", fileName);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for schedule report");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating schedule report");
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    [HttpGet("schedule/{clientId:guid}/preview")]
    public async Task<IActionResult> PreviewScheduleReport(
        Guid clientId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new GenerateScheduleReportCommand(clientId, fromDate, toDate, null);
            var pdf = await _mediator.Send(command, cancellationToken);

            return File(pdf, "application/pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating preview");
            return StatusCode(500, "An error occurred while generating the preview");
        }
    }
}

public record GenerateScheduleReportRequest(
    DateTime FromDate,
    DateTime ToDate,
    Guid? TemplateId = null);
