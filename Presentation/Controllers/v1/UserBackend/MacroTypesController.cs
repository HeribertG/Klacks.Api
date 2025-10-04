using Klacks.Api.Domain.Models.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class MacroTypesController : BaseController
{
    private readonly IMediator mediator;

    public MacroTypesController(IMediator mediator, ILogger<MacroTypesController> logger)
    {
        this.mediator = mediator;
    }

    [HttpDelete("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> DeleteMacroType(Guid id)
    {
        var macroType = await mediator.Send(new Application.Commands.Settings.MacrosTypes.DeleteCommand(id));
        if (macroType == null)
        {
            return NotFound();
        }
        return macroType;
    }

    [HttpGet("MacroType")]
    public async Task<IEnumerable<MacroType>> GetMacroTypes()
    {
        var result = await mediator.Send(new Application.Queries.Settings.MacrosTypes.ListQuery());
        return result;
    }

    [HttpGet("MacroType/{id}")]
    public async Task<ActionResult<MacroType>> GetMacroType(Guid id)
    {
        var macroType = await mediator.Send(new Application.Queries.Settings.MacrosTypes.GetQuery(id));

        if (macroType == null)
        {
            return NotFound();
        }
        return macroType;
    }

    [HttpPost("MacroType")]
    public async Task<MacroType> PostMacroType([FromBody] MacroType macroType)
    {
        var result = await mediator.Send(new Application.Commands.Settings.MacrosTypes.PostCommand(macroType));
        return result;
    }

    [HttpPut("MacroType")]
    public async Task<MacroType> PutMacroType([FromBody] MacroType macroType)
    {
        var result = await mediator.Send(new Application.Commands.Settings.MacrosTypes.PutCommand(macroType));
        return result;
    }
}
