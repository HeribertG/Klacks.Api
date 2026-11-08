// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Absences;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Klacks.Api.Application.DTOs.Schedules;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

/// <summary>
/// CRUD for the absence types (holiday, sick leave, training ...), the settings an employee absence is booked with,
/// plus the absence list reads and the Excel export. Writing a type is Admin-only, because absence types are settings
/// (owner decision 2026-11-08: all settings are Admin work); booking an employee's absence is planner work and goes
/// through BreaksController and BreakPlaceholdersController, not through this controller.
/// The Post and Put overrides only add the Admin restriction: an [Authorize] on an override is AND-combined with the
/// base Admin/Authorised attribute, so the effective rule is Admin. DELETE is Admin-only in the base already; reads
/// stay open, because planners and supervisors read these lists to plan.
/// </summary>
public class AbsencesController : InputBaseController<AbsenceResource>
{
    public AbsencesController(IMediator Mediator, ILogger<AbsencesController> logger)
          : base(Mediator, logger)
    {
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<AbsenceResource>> Post([FromBody] AbsenceResource resource) =>
        base.Post(resource);

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = Roles.Admin)]
    public override Task<ActionResult<AbsenceResource>> Put([FromBody] AbsenceResource resource) =>
        base.Put(resource);

    [HttpGet("CreateExcelFile/{language}")]
    public async Task<FileContentResult> CreateExcelFile([FromRoute] string language)
    {
        var res = await Mediator.Send(new CreateExcelFileQuery(language));
        if (res.Success)
        {
            string fileName = res.Messages;
            byte[] result = System.IO.File.ReadAllBytes(fileName);
            return File(result, "application/octet-stream", "Absences.xlsx");
        }
        else
        {
            return File(Encoding.UTF8.GetBytes(res.Messages), "text/plain");
        }
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AbsenceResource>>> GetAbsence()
    {
        var absences = await Mediator.Send(new ListQuery<AbsenceResource>());
        return Ok(absences);
    }

    [HttpGet("visible")]
    public async Task<ActionResult<IEnumerable<AbsenceResource>>> GetVisibleAbsences()
    {
        var absences = await Mediator.Send(new VisibleAbsencesQuery());
        return Ok(absences);
    }

    [HttpPost("GetSimpleAbsenceList")]
    public async Task<TruncatedAbsence> GetSimpleAbsenceList([FromBody] AbsenceFilter filter)
    {
        var truncatedAbsence = await Mediator.Send(new TruncatedListQuery(filter));
        return truncatedAbsence;
    }
}
