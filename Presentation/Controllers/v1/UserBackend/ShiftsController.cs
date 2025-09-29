using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Presentation.DTOs.Filter;
using Klacks.Api.Presentation.DTOs.Schedules;
using Klacks.Api.Presentation.DTOs.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class ShiftsController : InputBaseController<ShiftResource>
{
    private readonly ILogger<ShiftsController> logger;

    public ShiftsController(IMediator Mediator, ILogger<ShiftsController> logger)
            : base(Mediator, logger)
    {
        this.logger = logger;
    }

    [HttpPost("GetSimpleList")]
    public async Task<ActionResult<TruncatedShiftResource>> GetSimpleList([FromBody] ShiftFilter filter)
    {
        logger.LogInformation($"Fetching simple shift list with filter: {JsonConvert.SerializeObject(filter)}");
        var truncatedShifts = await Mediator.Send(new GetTruncatedListQuery(filter));
        logger.LogInformation($"Retrieved {truncatedShifts.Shifts.Count} truncated shift.");
        return Ok(truncatedShifts);
    }

    [HttpGet("CutList/{id}")]
    public async Task<IEnumerable<ShiftResource>> GetCutList(Guid id)
    {
        logger.LogInformation($"Fetching cut shift list with id: {id}");
        var truncatedShifts = await Mediator.Send(new CutListQuery(id));
        logger.LogInformation($"Retrieved {id}  cut shift list.");
        return truncatedShifts;
    }

    [HttpPost("Cuts")]
    public async Task<ActionResult<List<ShiftResource>>> PostCuts([FromBody] List<ShiftResource> cuts)
    {
        logger.LogInformation($"Creating {cuts.Count} cut shifts");
        var createdCuts = await Mediator.Send(new PostCutsCommand(cuts));
        logger.LogInformation($"Successfully created {createdCuts.Count} cut shifts");
        return Ok(createdCuts);
    }

    [HttpPut("Cuts")]
    public async Task<ActionResult<List<ShiftResource>>> PutCuts([FromBody] List<ShiftResource> cuts)
    {
        logger.LogInformation($"Updating {cuts.Count} cut shifts");
        var updatedCuts = await Mediator.Send(new PutCutsCommand(cuts));
        logger.LogInformation($"Successfully updated {updatedCuts.Count} cut shifts");
        return Ok(updatedCuts);
    }
}