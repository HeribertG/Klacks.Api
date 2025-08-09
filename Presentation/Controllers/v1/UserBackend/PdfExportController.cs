using Klacks.Api.Application.Queries.PdfExports;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

[ApiController]
[Route("api/[controller]")]
public class PdfExportController : ControllerBase
{
    private readonly IMediator mediator;

    public PdfExportController(IMediator mediator)
    {
        this.mediator = mediator;
    }

    [HttpGet("gantt-pdf")]
    public async Task<IActionResult> ExportToPdf([FromQuery] int year, [FromQuery] string pageFormat = "A3", [FromQuery] string language = "de")
    {
        var query = new GanttPdfExportQuery
        {
            Year = year,
            PageFormat = pageFormat,
            Language = language
        };

        var result = await mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }

        return File(result.PdfContent, result.ContentType, result.FileName);
    }
}
