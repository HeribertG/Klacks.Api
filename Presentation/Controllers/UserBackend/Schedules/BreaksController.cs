using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public class BreaksController : BaseController
{
    private readonly IMediator _mediator;

    public BreaksController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BreakResource>>> List()
    {
        var result = await _mediator.Send(new ListQuery<BreakResource>());
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BreakResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<BreakResource>(id));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<BreakResource>> Post([FromBody] BreakResource resource)
    {
        var model = await _mediator.Send(new PostCommand<BreakResource>(resource));
        return Ok(model);
    }

    [HttpPost("Bulk")]
    public async Task<ActionResult<BulkBreaksResponse>> BulkAdd([FromBody] BulkAddBreaksRequest request)
    {
        var response = await _mediator.Send(new BulkAddBreaksCommand(request));
        return Ok(response);
    }

    [HttpPut]
    public async Task<ActionResult<BreakResource>> Put([FromBody] BreakResource resource)
    {
        var model = await _mediator.Send(new PutCommand<BreakResource>(resource));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<BreakResource>> Delete(
        [FromRoute] Guid id,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd)
    {
        var model = await _mediator.Send(new DeleteBreakCommand(id, periodStart, periodEnd));
        if (model == null)
        {
            return NotFound();
        }
        return Ok(model);
    }

    [HttpDelete("Bulk")]
    public async Task<ActionResult<BulkBreaksResponse>> BulkDelete([FromBody] BulkDeleteBreaksRequest request)
    {
        var response = await _mediator.Send(new BulkDeleteBreaksCommand(request));
        return Ok(response);
    }
}
