// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.CalendarSelections;

/// <summary>
/// CRUD for calendar selections (merged country/state holiday calendars of contracts). Writing is Admin-only (owner
/// decision 2026-10-05, like CalendarRulesController): a selection decides which holidays are official for every
/// contract that uses it. The overrides only add the Admin restriction - [Authorize] on an override is AND-combined
/// with the base Admin/Authorised attribute, so the effective rule is Admin. Reads stay open.
/// </summary>
/// <param name="calendarSelectionRepository">Answers which selections are used by contracts</param>

public class CalendarSelectionsController : InputBaseController<CalendarSelectionResource>
{
    private readonly ICalendarSelectionRepository _calendarSelectionRepository;

    public CalendarSelectionsController(
        IMediator Mediator,
        ILogger<CalendarSelectionsController> logger,
        ICalendarSelectionRepository calendarSelectionRepository)
        : base(Mediator, logger)
    {
        _calendarSelectionRepository = calendarSelectionRepository;
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<CalendarSelectionResource>> Post([FromBody] CalendarSelectionResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<CalendarSelectionResource>> Put([FromBody] CalendarSelectionResource resource) =>
        base.Put(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<CalendarSelectionResource>> Delete(Guid id) =>
        base.Delete(id);

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CalendarSelectionResource>>> GetCalendarSelections()
    {
        var calendarSelections = await Mediator.Send(new ListQuery<CalendarSelectionResource>());
        return Ok(calendarSelections);
    }

    [HttpGet("used-by-contracts")]
    public async Task<ActionResult<IEnumerable<Guid>>> GetUsedByContracts()
    {
        var usedIds = await _calendarSelectionRepository.GetUsedByContractsAsync();
        return Ok(usedIds);
    }
}
