using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.DTOs.Reports;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Reports;

public class ScheduleReportController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<ScheduleReportController> _logger;

    public ScheduleReportController(IMediator mediator, ILogger<ScheduleReportController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("send")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<SendScheduleReportResponse>> Send(
        [FromForm] Guid clientId,
        [FromForm] string clientName,
        [FromForm] string startDate,
        [FromForm] string endDate,
        [FromForm] IFormFile pdfFile,
        CancellationToken cancellationToken)
    {
        if (pdfFile == null || pdfFile.Length == 0)
        {
            return BadRequest(new SendScheduleReportResponse
            {
                Success = false,
                ErrorMessage = "No PDF file provided"
            });
        }

        using var ms = new MemoryStream();
        await pdfFile.CopyToAsync(ms, cancellationToken);

        var command = new SendScheduleReportCommand(
            clientId,
            clientName,
            startDate,
            endDate,
            ms.ToArray(),
            pdfFile.FileName);

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}
