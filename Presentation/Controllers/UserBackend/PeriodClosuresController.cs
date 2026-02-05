using Klacks.Api.Application.Commands.PeriodClosures;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class PeriodClosuresController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IPeriodClosureRepository _repository;

    public PeriodClosuresController(IMediator mediator, IPeriodClosureRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<PeriodClosureResource>> Close([FromBody] ClosePeriodCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Reopen([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new ReopenPeriodCommand(id));
        if (!result)
        {
            return NotFound();
        }
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<List<PeriodClosureResource>>> GetByRange(
        [FromQuery] DateOnly start,
        [FromQuery] DateOnly end)
    {
        var closures = await _repository.GetByDateRange(start, end);
        var resources = closures.Select(c => new PeriodClosureResource
        {
            Id = c.Id,
            StartDate = c.StartDate,
            EndDate = c.EndDate,
            ClosedBy = c.ClosedBy,
            ClosedAt = c.ClosedAt
        }).ToList();

        return Ok(resources);
    }
}
