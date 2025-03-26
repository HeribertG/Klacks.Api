using Klacks.Api.Commands;
using Klacks.Api.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Klacks.Api.Controllers.V1.Backend;

[ApiController]
public abstract class InputBaseController<TModel> : BaseController
{
#pragma warning disable SA1401 // FieldsMustBePrivate
#pragma warning disable SA1307 // AccessibleFieldsMustBeginWithUpperCaseLetter
    public readonly IMediator mediator;
    private readonly ILogger<InputBaseController<TModel>> _logger;

    protected InputBaseController(IMediator mediator, ILogger<InputBaseController<TModel>> logger)
    {
        this.mediator = mediator;
        _logger = logger;
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<TModel>> Delete(Guid id)
    {
        try
        {
            _logger.LogInformation($"Attempting to delete {typeof(TModel).Name} with ID: {id}");

            var model = await mediator.Send(new DeleteCommand<TModel>(id));
            if (model == null)
            {
                _logger.LogWarning($"{typeof(TModel).Name} with ID: {id} not found for deletion.");
                return NotFound();
            }

            _logger.LogInformation($"{typeof(TModel).Name} with ID: {id} deleted successfully.");
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while deleting {typeof(TModel).Name} with ID: {id}");
            throw;
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TModel>> Get([FromRoute] Guid id)
    {
        try
        {
            _logger.LogInformation($"Fetching {typeof(TModel).Name} with ID: {id}");

            var model = await mediator.Send(new GetQuery<TModel>(id));

            if (model == null)
            {
                _logger.LogWarning($"{typeof(TModel).Name} with ID: {id} not found.");
                return NotFound();
            }

            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while fetching {typeof(TModel).Name} with ID: {id}");
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<TModel>> Post([FromBody] TModel resource)
    {
        try
        {
            _logger.LogInformation($"Creating new {typeof(TModel).Name} Resource: {JsonConvert.SerializeObject(resource)}");

            var model = await mediator.Send(new PostCommand<TModel>(resource));

            _logger.LogInformation($"{typeof(TModel).Name} created successfully.");
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while creating new {typeof(TModel).Name}");
            throw;
        }
    }

    [HttpPut]
    public async Task<ActionResult<TModel>> Put([FromBody] TModel resource)
    {
        try
        {
            _logger.LogInformation($"Updating {typeof(TModel).Name} Resource: {JsonConvert.SerializeObject(resource)}");

            var model = await mediator.Send(new PutCommand<TModel>(resource));

            if (model == null)
            {
                _logger.LogWarning($"{typeof(TModel).Name} not found for update.");
                return NotFound();
            }

            _logger.LogInformation($"{typeof(TModel).Name} updated successfully.");
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error occurred while updating {typeof(TModel).Name}");
            throw;
        }
    }
}
