using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
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
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<List<ShiftResource>>> PostBatchCuts([FromBody] PostBatchCutsRequest request)
    {
        var results = await Mediator.Send(new PostBatchCutsCommand(request.Operations));
        return Ok(results);
    }

    [HttpPost("Cuts/Reset")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<List<ShiftResource>>> PostResetCuts([FromBody] PostResetCutsRequest request)
    {
        var newStartDate = DateOnly.FromDateTime(request.NewStartDate);
        var results = await Mediator.Send(new PostResetCutsCommand(request.OriginalId, newStartDate));
        return Ok(results);
    }

    [HttpGet("Cuts/Reset/DateRange/{originalId}")]
    public async Task<ActionResult<ResetDateRangeResponse>> GetResetDateRange(Guid originalId)
    {
        var result = await Mediator.Send(new GetResetDateRangeQuery(originalId));
        return Ok(result);
    }
}