// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

/// <summary>
/// The generic CRUD controller. DELETE is Admin-only: under the granular rights model the Authorised
/// (supervisor) role holds CanCreate*/CanEdit* but no CanDelete* right at all, while the
/// Roles.Admin,Roles.Authorised gate this verb used to carry predates that model and was inherited
/// unchanged by every derived controller. Post and Put keep Admin/Authorised, which is the supervisor
/// write surface.
///
/// A resource whose DELETE genuinely is a supervisor action derives from SupervisorDeletableController
/// instead; an [Authorize] on an override is AND-combined with the base one and can never widen it.
/// </summary>
/// <typeparam name="TModel">The resource DTO this controller serves</typeparam>
[ApiController]
public abstract class InputBaseController<TModel> : BaseController, ICrudResourceController<TModel>
{
    protected readonly IMediator Mediator;

    protected InputBaseController(IMediator mediator, ILogger<InputBaseController<TModel>> logger)
    {
        this.Mediator = mediator;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.Admin)]
    public virtual async Task<ActionResult<TModel>> Delete(Guid id)
    {
        var model = await Mediator.Send(new DeleteCommand<TModel>(id));
        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TModel>> Get([FromRoute] Guid id)
    {
        var model = await Mediator.Send(new GetQuery<TModel>(id));

        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public virtual async Task<ActionResult<TModel>> Post([FromBody] TModel resource)
    {
        var model = await Mediator.Send(new PostCommand<TModel>(resource));
        return Ok(model);
    }

    [HttpPut]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
    public virtual async Task<ActionResult<TModel>> Put([FromBody] TModel resource)
    {
        var model = await Mediator.Send(new PutCommand<TModel>(resource));

        if (model == null)
        {
            return NotFound();
        }

        return Ok(model);
    }
}