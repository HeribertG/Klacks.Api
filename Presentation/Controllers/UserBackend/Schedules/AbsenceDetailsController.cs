using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class AbsenceDetailsController : InputBaseController<AbsenceDetailResource>
{
    public AbsenceDetailsController(IMediator mediator, ILogger<AbsenceDetailsController> logger)
        : base(mediator, logger)
    {
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceDetailResource>>> GetAbsenceDetails()
    {
        var absenceDetails = await Mediator.Send(new ListQuery<AbsenceDetailResource>());
        return Ok(absenceDetails);
    }
}
