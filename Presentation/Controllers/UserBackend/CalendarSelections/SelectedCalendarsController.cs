// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// CRUD for the single country/state calendars of a calendar selection. Writing is Admin-only (owner decision
/// 2026-10-05, like CalendarRulesController): a selected calendar and its OfficialOverride decide which holidays are
/// official for every contract using the selection. The overrides only add the Admin restriction - [Authorize] on an
/// override is AND-combined with the base Admin/Authorised attribute, so the effective rule is Admin. Reads stay open.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.CalendarSelections;

public class SelectedCalendarsController : InputBaseController<SelectedCalendarResource>
{
    public SelectedCalendarsController(IMediator Mediator, ILogger<SelectedCalendarsController> logger)
        : base(Mediator, logger)
    {
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<SelectedCalendarResource>> Post([FromBody] SelectedCalendarResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<SelectedCalendarResource>> Put([FromBody] SelectedCalendarResource resource) =>
        base.Put(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<SelectedCalendarResource>> Delete(Guid id) =>
        base.Delete(id);
}
