// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Qualifications;
using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries.Qualifications;
using Klacks.Api.Application.Queries.Shifts;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// Shift endpoints. Post and Put are overridden for AllowVariantToken only, so a shift can be created and saved in a
/// variant (key); route and roles stay those of InputBaseController, because an override inherits them and adds no
/// Authorize of its own. Delete takes no token, so it needs no such marker. Cuts/Batch and Cuts/Reset carry
/// AllowVariantToken as well, so the token a cut piece carries passes the token filter; both keep their roles
/// (Admin, Authorised), and ShiftCutFacade decides from the stored token whether the variant is open and visible.
/// </summary>
public class ShiftsController : InputBaseController<ShiftResource>
{
    public ShiftsController(IMediator Mediator, ILogger<ShiftsController> logger)
            : base(Mediator, logger)
    {
    }

    [AllowVariantToken]
    public override Task<ActionResult<ShiftResource>> Post([FromBody] ShiftResource resource) => base.Post(resource);

    [AllowVariantToken]
    public override Task<ActionResult<ShiftResource>> Put([FromBody] ShiftResource resource) => base.Put(resource);

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
    [AllowVariantToken]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<List<ShiftResource>>> PostBatchCuts([FromBody] PostBatchCutsRequest request)
    {
        var results = await Mediator.Send(new PostBatchCutsCommand(request.Operations));
        return Ok(results);
    }

    [HttpPost("Cuts/Reset")]
    [AllowVariantToken]
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

    [HttpPost("ByIds")]
    public async Task<ActionResult<List<ShiftResource>>> GetShiftsByIds([FromBody] List<Guid> ids)
    {
        var result = await Mediator.Send(new GetShiftsByIdsQuery(ids));
        return Ok(result);
    }

    [HttpPost("Schedule")]
    public async Task<ActionResult<ShiftScheduleResponse>> GetShiftSchedule([FromBody] ShiftScheduleFilter filter, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetShiftScheduleQuery(filter), cancellationToken);
        return Ok(result);
    }

    [HttpPost("Schedule/Partial")]
    public async Task<ActionResult<ShiftScheduleResponse>> GetShiftSchedulePartial([FromBody] ShiftSchedulePartialFilter filter, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetShiftSchedulePartialQuery(filter), cancellationToken);
        return Ok(result);
    }

    [HttpGet("RequiredQualifications/{shiftId}")]
    public async Task<ActionResult<List<ShiftRequiredQualificationResource>>> GetRequiredQualifications(Guid shiftId)
    {
        var result = await Mediator.Send(new GetShiftRequiredQualificationsQuery(shiftId));
        return Ok(result);
    }

    [HttpPost("RequiredQualifications")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult<Guid>> SetRequiredQualification(
        [FromBody] SetShiftRequiredQualificationRequest request)
    {
        var id = await Mediator.Send(new SetShiftRequiredQualificationCommand(
            request.ShiftId, request.QualificationId, request.IsMandatory, request.MinLevel, request.LevelBindingOverride));
        return Ok(id);
    }

    [HttpDelete("RequiredQualifications/{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public async Task<ActionResult> DeleteRequiredQualification(Guid id)
    {
        await Mediator.Send(new DeleteShiftRequiredQualificationCommand(id));
        return NoContent();
    }
}