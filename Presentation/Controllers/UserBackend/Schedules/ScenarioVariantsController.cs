// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Controller for variants (keys): unused collections of not yet active shifts that scenarios can carry. Reading is open
/// to every authenticated user; creating, changing and deleting needs Admin or Authorised, including the delete of a
/// single shift of a key (DeleteShift, 404 for a missing, hidden or consumed key and for a shift of another key). The JWT
/// scheme is inherited from BaseController.
/// </summary>
/// <param name="groupId">Group whose variants are listed</param>
/// <param name="id">Row id of the variant</param>
/// <param name="shiftId">Id of a row of the shift family to delete</param>
/// <param name="request">Name, description and time period of the variant, or what CopyShifts copies</param>

using Klacks.Api.Application.Commands.ScenarioVariants;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.ScenarioVariants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

[ApiController]
public class ScenarioVariantsController : BaseController
{
    private readonly IMediator _mediator;

    public ScenarioVariantsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<ActionResult<List<ScenarioVariantResource>>> GetList([FromQuery] Guid groupId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new ListScenarioVariantsQuery(groupId), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}/Shifts")]
    public async Task<ActionResult<List<ShiftResource>>> GetShifts(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetScenarioVariantShiftsQuery(id), cancellationToken);

        if (result == null)
        {
            return NotFound();
        }

        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<ScenarioVariantResource>> Create([FromBody] CreateScenarioVariantRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CreateScenarioVariantCommand(request), cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<ScenarioVariantResource>> Update(Guid id, [FromBody] UpdateScenarioVariantRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateScenarioVariantCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<bool>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteScenarioVariantCommand(id), cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Copies existing real shifts, each with its whole order family, into an unused variant. Either the shift ids or a
    /// source group must be given, exactly one of them; a whole group is copied with its subgroups and the result counts
    /// the families that were left out.
    /// </summary>
    /// <param name="id">Row id of the variant that receives the copies</param>
    /// <param name="request">The shift ids to copy or the source group</param>
    /// <param name="cancellationToken">Cancels the request</param>
    [HttpPost("{id}/CopyShifts")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<CopyShiftsToVariantResult>> CopyShifts(Guid id, [FromBody] CopyShiftsToVariantRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CopyShiftsToVariantCommand(id, request), cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id}/Shifts/{shiftId}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<bool>> DeleteShift(Guid id, Guid shiftId, CancellationToken cancellationToken)
    {
        var deleted = await _mediator.Send(new DeleteScenarioVariantShiftCommand(id, shiftId), cancellationToken);

        if (!deleted)
        {
            return NotFound();
        }

        return Ok(deleted);
    }
}
