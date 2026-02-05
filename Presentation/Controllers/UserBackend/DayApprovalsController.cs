using Klacks.Api.Application.Commands.DayApprovals;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class DayApprovalsController : BaseController
{
    private readonly IMediator _mediator;
    private readonly IDayApprovalRepository _repository;

    public DayApprovalsController(IMediator mediator, IDayApprovalRepository repository)
    {
        _mediator = mediator;
        _repository = repository;
    }

    [HttpPost]
    public async Task<ActionResult<DayApprovalResource>> Approve([FromBody] ApproveDayCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Revoke([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new RevokeDayApprovalCommand(id));
        if (!result)
        {
            return NotFound();
        }
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<List<DayApprovalResource>>> GetByRange(
        [FromQuery] DateOnly start,
        [FromQuery] DateOnly end,
        [FromQuery] Guid? groupId)
    {
        var approvals = await _repository.GetByDateRange(start, end, groupId);
        var resources = approvals.Select(a => new DayApprovalResource
        {
            Id = a.Id,
            ApprovalDate = a.ApprovalDate,
            GroupId = a.GroupId,
            ApprovedBy = a.ApprovedBy,
            ApprovedAt = a.ApprovedAt
        }).ToList();

        return Ok(resources);
    }
}
