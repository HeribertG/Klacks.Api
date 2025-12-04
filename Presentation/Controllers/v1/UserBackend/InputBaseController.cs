using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

[ApiController]
public abstract class InputBaseController<TModel> : BaseController
{
    protected readonly IMediator Mediator;

    protected InputBaseController(IMediator mediator, ILogger<InputBaseController<TModel>> logger)
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