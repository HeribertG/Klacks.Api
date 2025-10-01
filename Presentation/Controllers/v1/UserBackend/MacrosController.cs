using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class MacrosController : BaseController
{
    private readonly ILogger<MacrosController> logger;
    private readonly IMediator mediator;

    public MacrosController(IMediator mediator, ILogger<MacrosController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    [HttpDelete("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> DeleteMacro(Guid id)
    {
        logger.LogInformation($"Deleting Macro with ID: {id}");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.DeleteCommand(id));
        logger.LogInformation($"Macro with ID: {id} deleted successfully");
        return result;
    }

    [HttpGet("Macros")]
    public async Task<IEnumerable<MacroResource>> GetMacros()
    {
        logger.LogInformation("Fetching Macros list");
        var result = await mediator.Send(new Application.Queries.Settings.Macros.ListQuery());
        logger.LogInformation($"Retrieved {result.Count()} Macros");
        return result;
    }

    [HttpGet("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> GetMacro(Guid id)
    {
        logger.LogInformation($"Fetching Macro with ID: {id}");
        var macro = await mediator.Send(new Application.Queries.Settings.Macros.GetQuery(id));

        if (macro == null)
        {
            logger.LogWarning($"Macro with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"Macro with ID: {id} retrieved successfully");
        return macro;
    }

    [HttpPost("Macros")]
    public async Task<ActionResult<MacroResource>> PostMacro([FromBody] MacroResource macroResource)
    {
        logger.LogInformation("Creating new Macro");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PostCommand(macroResource));
        logger.LogInformation("Macro created successfully");
        return result;
    }

    [HttpPut("Macros")]
    public async Task<ActionResult<MacroResource>> PutMacro([FromBody] MacroResource macroResource)
    {
        logger.LogInformation("Updating Macro");
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PutCommand(macroResource));
        logger.LogInformation("Macro updated successfully");
        return result;
    }
}
