// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Minimal OAuth 2.1 authorization server for MCP clients: dynamic client registration
/// (RFC 7591), the authorization endpoint with an HTML login and consent form (PKCE S256,
/// public clients only) and the token endpoint that issues Klacks personal access tokens
/// as OAuth access tokens. Errors are never redirected to unvalidated redirect URIs.
/// </summary>

using System.Net.Mime;
using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Queries.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;

namespace Klacks.Api.Presentation.Controllers.OAuth;

[ApiController]
[AllowAnonymous]
[Route(OAuthConstants.RouteBase)]
public class OAuthAuthorizationServerController : ControllerBase
{
    private const string FormUrlEncodedContentType = "application/x-www-form-urlencoded";

    private readonly IMediator _mediator;
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<OAuthAuthorizationServerController> _logger;

    public OAuthAuthorizationServerController(
        IMediator mediator,
        IAntiforgery antiforgery,
        ILogger<OAuthAuthorizationServerController> logger)
    {
        _mediator = mediator;
        _antiforgery = antiforgery;
        _logger = logger;
    }

    [HttpPost(OAuthConstants.RegisterEndpointName)]
    public async Task<IActionResult> Register([FromBody] OAuthClientRegistrationRequest request)
    {
        var result = await _mediator.Send(new RegisterOAuthClientCommand(request));
        if (result.Error != null)
        {
            return BadRequest(result.Error);
        }

        _logger.LogInformation("OAuth client {ClientId} registered", result.Response!.ClientId);

        return StatusCode(StatusCodes.Status201Created, result.Response);
    }

    [HttpGet(OAuthConstants.AuthorizeEndpointName)]
    public async Task<IActionResult> Authorize(
        [FromQuery(Name = OAuthConstants.ParameterClientId)] string? clientId,
        [FromQuery(Name = OAuthConstants.ParameterRedirectUri)] string? redirectUri,
        [FromQuery(Name = OAuthConstants.ParameterResponseType)] string? responseType,
        [FromQuery(Name = OAuthConstants.ParameterCodeChallenge)] string? codeChallenge,
        [FromQuery(Name = OAuthConstants.ParameterCodeChallengeMethod)] string? codeChallengeMethod,
        [FromQuery(Name = OAuthConstants.ParameterScope)] string? scope,
        [FromQuery(Name = OAuthConstants.ParameterState)] string? state)
    {
        var validation = await _mediator.Send(new ValidateOAuthAuthorizeRequestQuery(
            clientId, redirectUri, responseType, codeChallenge, codeChallengeMethod));

        if (!validation.IsValid)
        {
            return AuthorizeValidationError(validation, state);
        }

        return RenderLoginPage(validation.ClientName!, clientId!, validation.EffectiveRedirectUri!, responseType!, codeChallenge!, codeChallengeMethod!, scope, state, errorMessage: null);
    }

    [HttpPost(OAuthConstants.AuthorizeEndpointName)]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting(RateLimitingPolicies.Login)]
    [Consumes(FormUrlEncodedContentType)]
    public async Task<IActionResult> Authorize(
        [FromForm(Name = OAuthConstants.ParameterEmail)] string? email,
        [FromForm(Name = OAuthConstants.ParameterPassword)] string? password,
        [FromForm(Name = OAuthConstants.ParameterDecision)] string? decision,
        [FromForm(Name = OAuthConstants.ParameterClientId)] string? clientId,
        [FromForm(Name = OAuthConstants.ParameterRedirectUri)] string? redirectUri,
        [FromForm(Name = OAuthConstants.ParameterResponseType)] string? responseType,
        [FromForm(Name = OAuthConstants.ParameterCodeChallenge)] string? codeChallenge,
        [FromForm(Name = OAuthConstants.ParameterCodeChallengeMethod)] string? codeChallengeMethod,
        [FromForm(Name = OAuthConstants.ParameterScope)] string? scope,
        [FromForm(Name = OAuthConstants.ParameterState)] string? state)
    {
        var validation = await _mediator.Send(new ValidateOAuthAuthorizeRequestQuery(
            clientId, redirectUri, responseType, codeChallenge, codeChallengeMethod));

        if (!validation.IsValid)
        {
            return AuthorizeValidationError(validation, state);
        }

        if (decision == OAuthConstants.DecisionDeny)
        {
            return RedirectWithError(validation.EffectiveRedirectUri!, OAuthConstants.ErrorAccessDenied, "The user denied the authorization request.", state);
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return RenderLoginPage(validation.ClientName!, clientId!, validation.EffectiveRedirectUri!, responseType!, codeChallenge!, codeChallengeMethod!, scope, state, "Email and password are required.");
        }

        var issueResult = await _mediator.Send(new IssueOAuthAuthorizationCodeCommand(
            email, password, clientId!, validation.EffectiveRedirectUri!, codeChallenge!, scope));

        if (issueResult.Error == OAuthConstants.ErrorAccessDenied)
        {
            return RenderLoginPage(validation.ClientName!, clientId!, validation.EffectiveRedirectUri!, responseType!, codeChallenge!, codeChallengeMethod!, scope, state, "Invalid email or password.");
        }

        if (issueResult.Error != null)
        {
            return BadRequest(new OAuthErrorResponse(issueResult.Error, issueResult.ErrorDescription!));
        }

        var query = new Dictionary<string, string?>
        {
            [OAuthConstants.ParameterCode] = issueResult.Code
        };
        AppendState(query, state);

        return Redirect(QueryHelpers.AddQueryString(validation.EffectiveRedirectUri!, query));
    }

    [HttpPost(OAuthConstants.TokenEndpointName)]
    [EnableRateLimiting(RateLimitingPolicies.Login)]
    [IgnoreAntiforgeryToken]
    [Consumes(FormUrlEncodedContentType)]
    public async Task<IActionResult> Token(
        [FromForm(Name = OAuthConstants.ParameterGrantType)] string? grantType,
        [FromForm(Name = OAuthConstants.ParameterCode)] string? code,
        [FromForm(Name = OAuthConstants.ParameterRedirectUri)] string? redirectUri,
        [FromForm(Name = OAuthConstants.ParameterClientId)] string? clientId,
        [FromForm(Name = OAuthConstants.ParameterCodeVerifier)] string? codeVerifier)
    {
        var result = await _mediator.Send(new ExchangeOAuthTokenCommand(grantType, code, redirectUri, clientId, codeVerifier));
        if (result.Error != null)
        {
            return StatusCode(result.ErrorStatusCode, result.Error);
        }

        return Ok(result.Response);
    }

    private IActionResult AuthorizeValidationError(OAuthAuthorizeValidationResult validation, string? state)
    {
        if (validation.CanRedirectError)
        {
            return RedirectWithError(validation.EffectiveRedirectUri!, validation.Error!, validation.ErrorDescription!, state);
        }

        return BadRequest(new OAuthErrorResponse(validation.Error!, validation.ErrorDescription!));
    }

    private IActionResult RedirectWithError(string redirectUri, string error, string errorDescription, string? state)
    {
        var query = new Dictionary<string, string?>
        {
            [OAuthConstants.ParameterError] = error,
            [OAuthConstants.ParameterErrorDescription] = errorDescription
        };
        AppendState(query, state);

        return Redirect(QueryHelpers.AddQueryString(redirectUri, query));
    }

    private static void AppendState(Dictionary<string, string?> query, string? state)
    {
        if (!string.IsNullOrEmpty(state))
        {
            query[OAuthConstants.ParameterState] = state;
        }
    }

    private IActionResult RenderLoginPage(
        string clientName,
        string clientId,
        string redirectUri,
        string responseType,
        string codeChallenge,
        string codeChallengeMethod,
        string? scope,
        string? state,
        string? errorMessage)
    {
        var antiforgeryTokens = _antiforgery.GetAndStoreTokens(HttpContext);

        var hiddenFields = new Dictionary<string, string>
        {
            [antiforgeryTokens.FormFieldName] = antiforgeryTokens.RequestToken ?? string.Empty,
            [OAuthConstants.ParameterClientId] = clientId,
            [OAuthConstants.ParameterRedirectUri] = redirectUri,
            [OAuthConstants.ParameterResponseType] = responseType,
            [OAuthConstants.ParameterCodeChallenge] = codeChallenge,
            [OAuthConstants.ParameterCodeChallengeMethod] = codeChallengeMethod,
            [OAuthConstants.ParameterScope] = scope ?? string.Empty,
            [OAuthConstants.ParameterState] = state ?? string.Empty
        };

        var formAction = $"/{OAuthConstants.RouteBase}/{OAuthConstants.AuthorizeEndpointName}";
        var html = OAuthLoginPageRenderer.Render(formAction, clientName, hiddenFields, errorMessage);

        return Content(html, MediaTypeNames.Text.Html);
    }
}
