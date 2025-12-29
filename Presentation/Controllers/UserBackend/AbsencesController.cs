using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class AbsencesController : InputBaseController<AbsenceResource>
{
    public AbsencesController(IMediator Mediator, ILogger<AbsencesController> logger)
          : base(Mediator, logger)
    {
    }

    [HttpGet("CreateExcelFile/{language}")]
    public async Task<FileContentResult> CreateExcelFile([FromRoute] string language)
    {
        var res = await Mediator.Send(new CreateExcelFileQuery(language));
        if (res.Success)
        {
            string fileName = res.Messages;
            byte[] result = System.IO.File.ReadAllBytes(fileName);
            return File(result, "application/octet-stream", "Absences.xlsx");
        }
        else
        {
            return File(Encoding.UTF8.GetBytes(res.Messages), "text/plain");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceResource>>> GetAbsence()
    {
        var absences = await Mediator.Send(new ListQuery<AbsenceResource>());
        return Ok(absences);
    }

    [HttpGet("visible")]
    public async Task<ActionResult<IEnumerable<AbsenceResource>>> GetVisibleAbsences()
    {
        var absences = await Mediator.Send(new VisibleAbsencesQuery());
        return Ok(absences);
    }

    [HttpPost("GetSimpleAbsenceList")]
    public async Task<TruncatedAbsence> GetSimpleAbsenceList([FromBody] AbsenceFilter filter)
    {
        var truncatedAbsence = await Mediator.Send(new TruncatedListQuery(filter));
        return truncatedAbsence;
    }
}
