// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

/// <summary>
/// CRUD for the states (cantons, regions) offered in addresses and calendars. Writing them is Admin-only, because they
/// are settings (owner decision 2026-11-08: all settings are Admin work).
/// The Post and Put overrides only add the Admin restriction: an [Authorize] on an override is AND-combined with the
/// base Admin/Authorised attribute, so the effective rule is Admin. DELETE is Admin-only in the base already; reads
/// stay open, because planners and supervisors read these lists to plan.
/// </summary>
public class StatesController : InputBaseController<StateResource>
{
    private readonly ILogger<StatesController> _logger;

    public StatesController(IMediator mediator, ILogger<StatesController> logger)
      : base(mediator, logger)
    {
        this._logger = logger;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<StateResource>> Post([FromBody] StateResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<StateResource>> Put([FromBody] StateResource resource) =>
        base.Put(resource);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StateResource>>> List() => this.Ok(await this.Mediator.Send(new ListQuery<StateResource>()));
}
