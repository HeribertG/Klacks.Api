// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// CRUD for the extended absences (details) of an absence type. Writing them is Admin-only, because they belong to
/// the absence type settings (owner decision 2026-11-08: all settings are Admin work).
/// The Post and Put overrides only add the Admin restriction: an [Authorize] on an override is AND-combined with the
/// base Admin/Authorised attribute, so the effective rule is Admin. DELETE is Admin-only in the base already; reads
/// stay open, because planners and supervisors read these lists to plan.
/// </summary>
public class AbsenceDetailsController : InputBaseController<AbsenceDetailResource>
{
    public AbsenceDetailsController(IMediator mediator, ILogger<AbsenceDetailsController> logger)
        : base(mediator, logger)
    {
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<AbsenceDetailResource>> Post([FromBody] AbsenceDetailResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<AbsenceDetailResource>> Put([FromBody] AbsenceDetailResource resource) =>
        base.Put(resource);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceDetailResource>>> GetAbsenceDetails()
    {
        var absenceDetails = await Mediator.Send(new ListQuery<AbsenceDetailResource>());
        return Ok(absenceDetails);
    }
}
