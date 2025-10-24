using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ShiftsController : InputBaseController<ShiftResource>
{
    public ShiftsController(IMediator Mediator, ILogger<ShiftsController> logger)
            : base(Mediator, logger)
    {
    }

    [HttpPost("GetSimpleList")]
    public async Task<ActionResult<TruncatedShiftResource>> GetSimpleList([FromBody] ShiftFilter filter)
    {
        var truncatedShifts = await Mediator.Send(new GetTruncatedListQuery(filter));
        return Ok(truncatedShifts);
    }

    [HttpGet("CutList/{id}")]
    public async Task<IEnumerable<ShiftResource>> GetCutList(Guid id)
    {
        var truncatedShifts = await Mediator.Send(new CutListQuery(id));
        return truncatedShifts;
    }

    [HttpPost("Cuts/Batch")]
    public async Task<ActionResult<List<ShiftResource>>> PostBatchCuts([FromBody] PostBatchCutsRequest request)
    {
        var results = await Mediator.Send(new PostBatchCutsCommand(request.Operations));
        return Ok(results);
    }
}