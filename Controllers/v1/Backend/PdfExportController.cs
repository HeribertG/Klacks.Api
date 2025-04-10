using Klacks.Api.Queries.PdfExports;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Controllers.v1.Backend;

[ApiController]
[Route("api/[controller]")]
public class PdfExportController : ControllerBase
{
    private readonly IMediator _mediator;

    public PdfExportController(IMediator mediator)
    {
        _mediator = mediator;
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

        var result = await _mediator.Send(query);

        if (!result.Success)
        {
            return BadRequest(result.ErrorMessage);
        }

        return File(result.PdfContent, result.ContentType, result.FileName);
    }
}
