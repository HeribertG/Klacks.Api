// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Queries;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Scheduling;

/// <summary>
/// CRUD for period cap rules. Writing is Admin-only, because these rules are settings (owner decision 2026-11-07: roles are work
/// areas, Admin holds every setting; the skills already required CanEditSettings). The Post and Put overrides only add
/// the Admin restriction: an [Authorize] on an override is AND-combined with the base Admin/Authorised attribute, so the
/// effective rule is Admin. DELETE is Admin-only in the base already; reads stay open.
/// </summary>
public class PeriodCapRulesController : InputBaseController<PeriodCapRuleResource>
{
    private readonly ILogger<PeriodCapRulesController> _logger;

    public PeriodCapRulesController(IMediator mediator, ILogger<PeriodCapRulesController> logger)
        : base(mediator, logger)
    {
        _logger = logger;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<PeriodCapRuleResource>> Post([FromBody] PeriodCapRuleResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<PeriodCapRuleResource>> Put([FromBody] PeriodCapRuleResource resource) =>
        base.Put(resource);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PeriodCapRuleResource>>> GetAll()
    {
        var rules = await Mediator.Send(new ListQuery<PeriodCapRuleResource>());
        return Ok(rules);
    }
}
