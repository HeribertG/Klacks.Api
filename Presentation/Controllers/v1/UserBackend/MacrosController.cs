using Klacks.Api.Presentation.DTOs.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class MacrosController : BaseController
{
    private readonly IMediator mediator;

    public MacrosController(IMediator mediator, ILogger<MacrosController> logger)
    {
        this.mediator = mediator;
    }

    [HttpDelete("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> DeleteMacro(Guid id)
    {
        var result = await mediator.Send(new Application.Commands.Settings.Macros.DeleteCommand(id));
        return result;
    }

    [HttpGet("Macros")]
    public async Task<IEnumerable<MacroResource>> GetMacros()
    {
        var result = await mediator.Send(new Application.Queries.Settings.Macros.ListQuery());
        return result;
    }

    [HttpGet("Macros/{id}")]
    public async Task<ActionResult<MacroResource>> GetMacro(Guid id)
    {
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
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PostCommand(macroResource));
        return result;
    }

    [HttpPut("Macros")]
    public async Task<ActionResult<MacroResource>> PutMacro([FromBody] MacroResource macroResource)
    {
        var result = await mediator.Send(new Application.Commands.Settings.Macros.PutCommand(macroResource));
        return result;
    }
}
