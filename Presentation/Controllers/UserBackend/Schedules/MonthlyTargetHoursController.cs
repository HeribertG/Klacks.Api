// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// CRUD for the monthly target hours table the period target is read from. Writing it is Admin-only, because the table
/// is a setting (owner decision 2026-11-08: all settings are Admin work).
/// The Post and Put overrides only add the Admin restriction: an [Authorize] on an override is AND-combined with the
/// base Admin/Authorised attribute, so the effective rule is Admin. DELETE is Admin-only in the base already; reads
/// stay open.
/// </summary>
public class MonthlyTargetHoursController : InputBaseController<MonthlyTargetHoursResource>
{
    private readonly ILogger<MonthlyTargetHoursController> _logger;

    public MonthlyTargetHoursController(IMediator mediator, ILogger<MonthlyTargetHoursController> logger)
      : base(mediator, logger)
    {
        this._logger = logger;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<MonthlyTargetHoursResource>> Post([FromBody] MonthlyTargetHoursResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<MonthlyTargetHoursResource>> Put([FromBody] MonthlyTargetHoursResource resource) =>
        base.Put(resource);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MonthlyTargetHoursResource>>> List() => this.Ok(await this.Mediator.Send(new ListQuery<MonthlyTargetHoursResource>()));
}
