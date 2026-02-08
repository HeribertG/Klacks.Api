using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.Queries.LLM;
using Klacks.Api.Application.Commands.LLM;
using Klacks.Api.Application.Mappers;

namespace Klacks.Api.Presentation.Controllers.Assistant;

[ApiController]
[Route("api/backend/assistant/providers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProvidersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly LLMMapper _mapper = new();

    public ProvidersController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetProviders()
    {
        var providers = await _mediator.Send(new GetProvidersQuery());
        return Ok(_mapper.ToProviderResources(providers));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetProvider(Guid id)
    {
        var provider = await _mediator.Send(new GetProviderByIdQuery(id));
        if (provider == null)
        {
            return NotFound();
        }

        return Ok(_mapper.ToProviderResource(provider));
    }

    [HttpPost]
    public async Task<IActionResult> CreateProvider([FromBody] CreateProviderCommand command)
    {
        var provider = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetProvider), new { id = provider.Id }, _mapper.ToProviderResource(provider));
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

        return Ok(_mapper.ToProviderResource(provider));
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