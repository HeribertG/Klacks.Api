using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class MacrosController : BaseController
{
    private readonly ILogger<MacrosController> _logger;
    private readonly IMediator mediator;

    public MacrosController(IMediator mediator, ILogger<MacrosController> logger)
    {
        this.mediator = mediator;
        this._logger = logger;
    }

    [HttpDelete("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> DeleteMacro(Guid id)
    {
        _logger.LogInformation($"Deleting Macro with ID: {id}");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.DeleteCommand(id));
        _logger.LogInformation($"Macro with ID: {id} deleted successfully");
        return result;
    }

    [HttpGet("Macros")]
    public async Task<IEnumerable<MacroResource>> GetMacros()
    {
        _logger.LogInformation("Fetching Macros list");
        var result = await mediator.Send(new Application.Queries.Settings.Macros.ListQuery());
        _logger.LogInformation($"Retrieved {result.Count()} Macros");
        return result;
    }

    [HttpGet("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> GetMacro(Guid id)
    {
        _logger.LogInformation($"Fetching Macro with ID: {id}");
        var macro = await mediator.Send(new Application.Queries.Settings.Macros.GetQuery(id));

        if (macro == null)
        {
            _logger.LogWarning($"Macro with ID: {id} not found");
            return NotFound();
        }
        _logger.LogInformation($"Macro with ID: {id} retrieved successfully");
        return macro;
    }

    [HttpPost("Macros")]
    public async Task<ActionResult<MacroResource>> PostMacro([FromBody] MacroResource macroResource)
    {
        _logger.LogInformation("Creating new Macro");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PostCommand(macroResource));
        _logger.LogInformation("Macro created successfully");
        return result;
    }

    [HttpPut("Macros")]
    public async Task<ActionResult<MacroResource>> PutMacro([FromBody] MacroResource macroResource)
    {
        _logger.LogInformation("Updating Macro");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PutCommand(macroResource));
        _logger.LogInformation("Macro updated successfully");
        return result;
    }
}
