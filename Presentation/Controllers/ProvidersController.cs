using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using Klacks.Api.Application.Queries.LLM;
using Klacks.Api.Application.Commands.LLM;

namespace Klacks.Api.Presentation.Controllers;

[ApiController]
[Route("api/v1/backend/assistant/providers")]
[Authorize]
public class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _mediator.Send(new GetProvidersQuery());
        return Ok(providers);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProvider(Guid id)
    {
        var provider = await _mediator.Send(new GetProviderByIdQuery(id));
        if (provider == null)
        {
            return NotFound();
        }

        return Ok(provider);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProvider([FromBody] CreateProviderCommand command)
    {
        var provider = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProvider), new { id = provider.Id }, provider);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProvider(Guid id, [FromBody] UpdateProviderCommand command)
    {
        command.Id = id;
        var provider = await _mediator.Send(command);
        if (provider == null)
        {
            return NotFound();
        }

        return Ok(provider);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProvider(Guid id)
    {
        var success = await _mediator.Send(new DeleteProviderCommand(id));
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}