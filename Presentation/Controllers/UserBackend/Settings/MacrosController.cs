using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Settings;

public class MacrosController : BaseController
{
    private readonly IMediator mediator;
    private readonly ILogger<MacrosController> logger;

    public MacrosController(IMediator mediator, ILogger<MacrosController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    [HttpDelete("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> DeleteMacro(Guid id)
    {
        logger.LogInformation("[MACRO-API] DELETE Macros/{Id}", id);
        var result = await mediator.Send(new Application.Commands.Settings.Macros.DeleteCommand(id));
        return result;
    }

    [HttpGet("Macros")]
    public async Task<IEnumerable<MacroResource>> GetMacros()
    {
        logger.LogInformation("[MACRO-API] GET Macros");
        var result = await mediator.Send(new Application.Queries.Settings.Macros.ListQuery());
        return result;
    }

    [HttpGet("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> GetMacro(Guid id)
    {
        logger.LogInformation("[MACRO-API] GET Macros/{Id}", id);
        var macro = await mediator.Send(new Application.Queries.Settings.Macros.GetQuery(id));

        if (macro == null)
        {
            return NotFound();
        }
        return macro;
    }

    [HttpPost("Macros")]
    public async Task<ActionResult<MacroResource>> PostMacro([FromBody] MacroResource macroResource)
    {
        logger.LogInformation("[MACRO-API] POST Macros - Name: {Name}", macroResource.Name);
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PostCommand(macroResource));
        return result;
    }

    [HttpPut("Macros")]
    public async Task<ActionResult<MacroResource>> PutMacro([FromBody] MacroResource macroResource)
    {
        logger.LogInformation("[MACRO-API] PUT Macros - Id: {Id}, Name: {Name}", macroResource.Id, macroResource.Name);
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PutCommand(macroResource));
        return result;
    }
}
