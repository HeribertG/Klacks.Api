using Klacks.Api.Application.Commands.OAuth2;
using Klacks.Api.Application.Queries.OAuth2;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.OAuth2;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend.Authentification;

[ApiController]
[Route("api/backend/[controller]")]
public class OAuth2Controller : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<OAuth2Controller> _logger;

    public OAuth2Controller(IMediator mediator, ILogger<OAuth2Controller> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("providers")]
    public async Task<IActionResult> GetOAuth2Providers()
    {
        var result = await _mediator.Send(new GetOAuth2ProvidersQuery());
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("logout-url/{providerId}")]
    public async Task<IActionResult> GetLogoutUrl(Guid providerId, [FromQuery] string? postLogoutRedirectUri)
    {
        var result = await _mediator.Send(new GetOAuth2LogoutUrlQuery(providerId, postLogoutRedirectUri));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("authorize/{providerId}")]
    public async Task<IActionResult> Authorize(Guid providerId, [FromQuery] string redirectUri)
    {
        var result = await _mediator.Send(new GetOAuth2AuthorizeQuery(providerId, redirectUri));
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] OAuth2CallbackRequest request)
    {
        var result = await _mediator.Send(new OAuth2CallbackCommand(request));
        return Ok(result);
    }
}
