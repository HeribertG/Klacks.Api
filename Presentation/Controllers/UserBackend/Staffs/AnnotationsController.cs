// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// CRUD for the free-text notes attached to a client. Deliberately not an InputBaseController: its
/// write verbs are pinned to Admin and Authorised, and notes are the one client-side write a caller
/// without any role (Planer) must be able to perform. Attributes on an override are AND-combined
/// with the base method's, so the restriction cannot be widened by overriding — the controller has
/// to declare its own verbs. Every write is gated on CanEditClientNotes in the body instead.
/// </summary>
/// <param name="mediator">Dispatches the annotation queries and commands</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.Queries.Annotation;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Staffs;

[ApiController]
public class AnnotationsController : BaseController, ICrudResourceController<AnnotationResource>
{
    private readonly IMediator _mediator;

    public AnnotationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetAnnotation()
    {
        var annotations = await _mediator.Send(new ListQuery<AnnotationResource>());
        return Ok(annotations);
    }

    [HttpGet("GetSimpleAnnotation/{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<IEnumerable<AnnotationResource>>> GetSimpleAnnotation(Guid id)
    {
        var annotations = await _mediator.Send(new GetSimpleListQuery(id));
        return Ok(annotations);
    }

    [HttpGet("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AnnotationResource>> Get([FromRoute] Guid id)
    {
        var model = await _mediator.Send(new GetQuery<AnnotationResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AnnotationResource>> Post([FromBody] AnnotationResource resource)
    {
        if (!Permissions.HasPermission(User.GetUserRights(), Permissions.CanEditClientNotes))
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var model = await _mediator.Send(new PostCommand<AnnotationResource>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AnnotationResource>> Put([FromBody] AnnotationResource resource)
    {
        if (!Permissions.HasPermission(User.GetUserRights(), Permissions.CanEditClientNotes))
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var model = await _mediator.Send(new PutCommand<AnnotationResource>(resource));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpDelete("{id}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public async Task<ActionResult<AnnotationResource>> Delete(Guid id)
    {
        if (!Permissions.HasPermission(User.GetUserRights(), Permissions.CanEditClientNotes))
        {
            return Forbid(JwtBearerDefaults.AuthenticationScheme);
        }

        var model = await _mediator.Send(new DeleteCommand<AnnotationResource>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}
