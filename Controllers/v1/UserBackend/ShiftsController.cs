using Klacks.Api.Queries.Shifts;
using Klacks.Api.Resources.Filter;
using Klacks.Api.Resources.Schedules;
using Klacks.Api.Resources.Staffs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Controllers.V1.UserBackend;

public class ShiftsController : InputBaseController<ShiftResource>
{
    private readonly ILogger<ShiftsController> logger;

    public ShiftsController(IMediator mediator, ILogger<ShiftsController> logger)
            : base(mediator, logger)
    {
        this.logger = logger;
    }

    [HttpPost("GetSimpleList")]
    public async Task<ActionResult<TruncatedShiftResource>> GetSimpleList([FromBody] ShiftFilter filter)
    {
        try
        {
            logger.LogInformation($"Fetching simple shift list with filter: {JsonConvert.SerializeObject(filter)}");
            var truncatedShifts = await mediator.Send(new GetTruncatedListQuery(filter));
            logger.LogInformation($"Retrieved {truncatedShifts.Shifts.Count} truncated shift.");
            return Ok(truncatedShifts);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred while fetching simple shift list.");
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    [HttpGet("CutList/{id}")]
    public async Task<IEnumerable<ShiftResource>> GetCutList(Guid id)
    {

    }

}
