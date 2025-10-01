using Klacks.Api.Domain.Models.Settings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

public class VatsController : BaseController
{
    private readonly ILogger<VatsController> logger;
    private readonly IMediator mediator;

    public VatsController(IMediator mediator, ILogger<VatsController> logger)
    {
        this.mediator = mediator;
        this.logger = logger;
    }

    [HttpDelete("Vat/{id}")]
    public async Task<ActionResult<Vat>> DeleteVat(Guid id)
    {
        logger.LogInformation($"Deleting VAT with ID: {id}");
        var vat = await mediator.Send(new Application.Commands.Settings.Vats.DeleteCommand(id));
        if (vat == null)
        {
            logger.LogWarning($"VAT with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"VAT with ID: {id} deleted successfully");
        return vat;
    }

    [HttpGet("Vat")]
    public async Task<IEnumerable<Vat>> GetVats()
    {
        logger.LogInformation("Fetching VAT list");
        var result = await mediator.Send(new Application.Queries.Settings.Vats.ListQuery());
        logger.LogInformation($"Retrieved {result.Count()} VATs");
        return result;
    }

    [HttpGet("Vat/{id}")]
    public async Task<ActionResult<Vat>> GetVat(Guid id)
    {
        logger.LogInformation($"Fetching VAT with ID: {id}");
        var vat = await mediator.Send(new Application.Queries.Settings.Vats.GetQuery(id));

        if (vat == null)
        {
            logger.LogWarning($"VAT with ID: {id} not found");
            return NotFound();
        }
        logger.LogInformation($"VAT with ID: {id} retrieved successfully");
        return vat;
    }

    [HttpPost("Vat")]
    public async Task<ActionResult<Vat>> PostVat([FromBody] Vat vat)
    {
        logger.LogInformation("Creating new VAT");
        var result = await mediator.Send(new Application.Commands.Settings.Vats.PostCommand(vat));
        logger.LogInformation("VAT created successfully");
        return result;
    }

    [HttpPut("Vat")]
    public async Task<ActionResult<Vat>> PutVat([FromBody] Vat vat)
    {
        logger.LogInformation("Updating VAT");
        var result = await mediator.Send(new Application.Commands.Settings.Vats.PutCommand(vat));
        logger.LogInformation("VAT updated successfully");
        return result;
    }
}
