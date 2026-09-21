// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The generic CRUD controller for resources whose DELETE is a supervisor action rather than an
/// administrative one. It exists because an [Authorize] on an override is AND-combined with the base
/// method's and can never lift it (AuthorizeAttributeInheritanceTests): once InputBaseController.Delete
/// became Admin-only, a controller that has to keep Authorised on DELETE could not stay on that base at
/// all. Everything else is identical to InputBaseController, including the Admin/Authorised gate on Post
/// and Put, so moving a controller between the two bases changes the DELETE verb and nothing else.
///
/// Membership of this base is not a convenience. A controller belongs here only with a flow that was
/// found in the code: GroupItems is removed by remove_client_from_group (CanEditClients) and
/// remove_shift_from_group (CanEditShifts) through the self API under the caller's own token, and its
/// sibling action RemoveByClientAndGroup is pinned as a supervisor action by spec 2.3; ScheduleNotes,
/// ScheduleCommands and Expenses are deleted from the schedule context menu, which carries no permission
/// gate of its own and is a supervisor's daily work.
/// </summary>
/// <typeparam name="TModel">The resource DTO this controller serves</typeparam>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
public abstract class SupervisorDeletableController<TModel> : BaseController, ICrudResourceController<TModel>
{
    protected readonly IMediator Mediator;

    protected SupervisorDeletableController(IMediator mediator, ILogger<SupervisorDeletableController<TModel>> logger)
    {
        this.Mediator = mediator;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = $"{Roles.Admin},{Roles.Authorised}")]
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
