using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

[ApiController]
public abstract class InputBaseController<TModel> : BaseController
{
    protected readonly IMediator Mediator;
    protected readonly ILogger<InputBaseController<TModel>> _logger;

    protected InputBaseController(IMediator mediator, ILogger<InputBaseController<TModel>> logger)
    {
        this.Mediator = mediator;
        this._logger = logger;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<TModel>> Delete(Guid id)
    {
        _logger.LogInformation($"Attempting to delete {typeof(TModel).Name} with ID: {id}");

        var model = await Mediator.Send(new DeleteCommand<TModel>(id));
        if (model == null)
        {
            _logger.LogWarning($"{typeof(TModel).Name} with ID: {id} not found for deletion.");
            return NotFound();
        }

        _logger.LogInformation($"{typeof(TModel).Name} with ID: {id} deleted successfully.");
        return Ok(model);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TModel>> Get([FromRoute] Guid id)
    {
        _logger.LogInformation($"Fetching {typeof(TModel).Name} with ID: {id}");

        var model = await Mediator.Send(new GetQuery<TModel>(id));

        if (model == null)
        {
            _logger.LogWarning($"{typeof(TModel).Name} with ID: {id} not found.");
            return NotFound();
        }

        return Ok(model);
    }

    [HttpPost]
    public async Task<ActionResult<TModel>> Post([FromBody] TModel resource)
    {
        _logger.LogInformation($"Creating new {typeof(TModel).Name} Resource: {JsonConvert.SerializeObject(resource)}");

        var model = await Mediator.Send(new PostCommand<TModel>(resource));

        _logger.LogInformation($"{typeof(TModel).Name} created successfully.");
        return Ok(model);
    }

    [HttpPut]
    public async Task<ActionResult<TModel>> Put([FromBody] TModel resource)
    {
        _logger.LogInformation($"Updating {typeof(TModel).Name} Resource: {JsonConvert.SerializeObject(resource)}");

        var model = await Mediator.Send(new PutCommand<TModel>(resource));

        if (model == null)
        {
            _logger.LogWarning($"{typeof(TModel).Name} not found for update.");
            return NotFound();
        }

        _logger.LogInformation($"{typeof(TModel).Name} updated successfully.");
        return Ok(model);
    }
}