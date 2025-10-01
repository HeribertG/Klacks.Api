using Klacks.Api.Domain.Models.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class MacroTypesController : BaseController
{
    private readonly ILogger<MacroTypesController> logger;
    private readonly IMediator mediator;

    public MacroTypesController(IMediator mediator, ILogger<MacroTypesController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    [HttpDelete("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> DeleteMacroType(Guid id)
    {
        logger.LogInformation($"Deleting MacroType with ID: {id}");
        var macroType = await mediator.Send(new Application.Commands.Settings.MacrosTypes.DeleteCommand(id));
        if (macroType == null)
        {
            logger.LogWarning($"MacroType with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"MacroType with ID: {id} deleted successfully");
        return macroType;
    }

    [HttpGet("MacroType")]
    public async Task<IEnumerable<MacroType>> GetMacroTypes()
    {
        logger.LogInformation("Fetching MacroType list");
        var result = await mediator.Send(new Application.Queries.Settings.MacrosTypes.ListQuery());
        logger.LogInformation($"Retrieved {result.Count()} MacroTypes");
        return result;
    }

    [HttpGet("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> GetMacroType(Guid id)
    {
        logger.LogInformation($"Fetching MacroType with ID: {id}");
        var macroType = await mediator.Send(new Application.Queries.Settings.MacrosTypes.GetQuery(id));

        if (macroType == null)
        {
            logger.LogWarning($"MacroType with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"MacroType with ID: {id} retrieved successfully");
        return macroType;
    }

    [HttpPost("MacroType")]
    public async Task<MacroType> PostMacroType([FromBody] MacroType macroType)
    {
        logger.LogInformation("Creating new MacroType");
        var result = await mediator.Send(new Application.Commands.Settings.MacrosTypes.PostCommand(macroType));
        logger.LogInformation("MacroType created successfully");
        return result;
    }

    [HttpPut("MacroType")]
    public async Task<MacroType> PutMacroType([FromBody] MacroType macroType)
    {
        logger.LogInformation("Updating MacroType");
        var result = await mediator.Send(new Application.Commands.Settings.MacrosTypes.PutCommand(macroType));
        logger.LogInformation("MacroType updated successfully");
        return result;
    }
}
