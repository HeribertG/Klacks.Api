using Klacks.Api.Application.Commands.IdentityProviders;
using Klacks.Api.Application.Queries.IdentityProviders;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.DTOs.IdentityProviders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[Authorize(Roles = "Admin")]
public class IdentityProvidersController : BaseController
{
    private readonly IMediator _mediator;
    private readonly ILogger<IdentityProvidersController> _logger;

    public IdentityProvidersController(IMediator mediator, ILogger<IdentityProvidersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("IdentityProviders")]
    public async Task<IEnumerable<IdentityProviderListResource>> GetIdentityProviders()
    {
        _logger.LogInformation("[IDENTITY-PROVIDER-API] GET IdentityProviders");
        return await _mediator.Send(new ListQuery());
    }

    [HttpGet("IdentityProviders/{id}")]
    public async Task<ActionResult<IdentityProviderResource>> GetIdentityProvider(Guid id)
    {
        _logger.LogInformation("[IDENTITY-PROVIDER-API] GET IdentityProviders/{Id}", id);
        var result = await _mediator.Send(new GetQuery(id));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpPost("IdentityProviders")]
    public async Task<ActionResult<IdentityProviderResource>> PostIdentityProvider([FromBody] IdentityProviderResource resource)
    {
        _logger.LogInformation("[IDENTITY-PROVIDER-API] POST IdentityProviders - Name: {Name}", resource.Name);
        var result = await _mediator.Send(new PostCommand(resource));
        return result!;
    }

    [HttpPut("IdentityProviders")]
    public async Task<ActionResult<IdentityProviderResource>> PutIdentityProvider([FromBody] IdentityProviderResource resource)
    {
        _logger.LogInformation("[IDENTITY-PROVIDER-API] PUT IdentityProviders - Id: {Id}, Name: {Name}", resource.Id, resource.Name);
        var result = await _mediator.Send(new PutCommand(resource));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpDelete("IdentityProviders/{id}")]
    public async Task<ActionResult<IdentityProviderResource>> DeleteIdentityProvider(Guid id)
    {
        _logger.LogInformation("[IDENTITY-PROVIDER-API] DELETE IdentityProviders/{Id}", id);
        var result = await _mediator.Send(new DeleteCommand(id));

        if (result == null)
        {
            return NotFound();
        }

        return result;
    }

    [HttpPost("IdentityProviders/{id}/TestConnection")]
    public async Task<ActionResult<TestConnectionResultResource>> TestConnection(Guid id)
    {
        _logger.LogInformation("[IDENTITY-PROVIDER-API] POST IdentityProviders/{Id}/TestConnection", id);
        var result = await _mediator.Send(new TestConnectionCommand(id));
        return result;
    }

    [HttpPost("IdentityProviders/{id}/SyncClients")]
    public async Task<ActionResult<IdentityProviderSyncResultResource>> SyncClients(Guid id)
    {
        _logger.LogInformation("[IDENTITY-PROVIDER-API] POST IdentityProviders/{Id}/SyncClients", id);
        var result = await _mediator.Send(new SyncClientsCommand(id));
        return result;
    }
}
