using Klacks.Api.Domain.Models.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class VatsController : BaseController
{
    private readonly IMediator mediator;

    public VatsController(IMediator mediator, ILogger<VatsController> logger)
    {
        this.mediator = mediator;
    }

    [HttpDelete("Vat/{id}")]
    public async Task<ActionResult<Vat>> DeleteVat(Guid id)
    {
        var vat = await mediator.Send(new Application.Commands.Settings.Vats.DeleteCommand(id));
        if (vat == null)
        {
            return NotFound();
        }
        return vat;
    }

    [HttpGet("Vat")]
    public async Task<IEnumerable<Vat>> GetVats()
    {
        var result = await mediator.Send(new Application.Queries.Settings.Vats.ListQuery());
        return result;
    }

    [HttpGet("Vat/{id}")]
    public async Task<ActionResult<Vat>> GetVat(Guid id)
    {
        var vat = await mediator.Send(new Application.Queries.Settings.Vats.GetQuery(id));

        if (vat == null)
        {
            return NotFound();
        }

        return vat;
    }

    [HttpPost("Vat")]
    public async Task<ActionResult<Vat>> PostVat([FromBody] Vat vat)
    {
        var result = await mediator.Send(new Application.Commands.Settings.Vats.PostCommand(vat));
        return result;
    }

    [HttpPut("Vat")]
    public async Task<ActionResult<Vat>> PutVat([FromBody] Vat vat)
    {
        var result = await mediator.Send(new Application.Commands.Settings.Vats.PutCommand(vat));
        return result;
    }
}
