// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Minimal OAuth 2.1 authorization server for MCP clients: dynamic client registration
/// (RFC 7591), the authorization endpoint with an HTML login and consent form (PKCE S256,
/// public clients only) and the token endpoint that issues Klacks personal access tokens
/// as OAuth access tokens. Errors are never redirected to unvalidated redirect URIs.
/// </summary>

using System.Net.Mime;
using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Interfaces.OAuth;
using Klacks.Api.Application.Services.OAuth;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Queries.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Presentation.Authentication;
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
    private readonly IOAuthConsentTextProvider _consentTexts;
    private readonly ILogger<OAuthAuthorizationServerController> _logger;

    public OAuthAuthorizationServerController(
        IMediator mediator,
        IAntiforgery antiforgery,
        IOAuthConsentTextProvider consentTexts,
        ILogger<OAuthAuthorizationServerController> logger)
    {
        _mediator = mediator;
        _antiforgery = antiforgery;
        _consentTexts = consentTexts;
        _logger = logger;
    }

    [HttpPost(OAuthConstants.RegisterEndpointName)]
    [EnableRateLimiting(RateLimitingPolicies.Login)]
    public async Task<IActionResult> Register([FromBody] OAuthClientRegistrationRequest request)
    {
        var result = await _mediator.Send(new RegisterOAuthClientCommand(request));
        if (result.Error != null)
        {
            return StatusCode(result.ErrorStatusCode, result.Error);
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
        [FromQuery(Name = OAuthConstants.ParameterState)] string? state)
    {
        var validation = await _mediator.Send(new ValidateOAuthAuthorizeRequestQuery(
            clientId, redirectUri, responseType, codeChallenge, codeChallengeMethod));

        if (!validation.IsValid)
        {
            return AuthorizeValidationError(validation, state);
        }

        return await RenderConsentPageAsync(validation, new ConsentRequest(clientId!, responseType!, codeChallenge!, codeChallengeMethod!, state), errorFor: null);
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
        [FromForm(Name = OAuthConstants.ParameterAccessMode)] string? accessMode,
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

        if (decision != OAuthConstants.DecisionApprove)
        {
            return BadRequest(new OAuthErrorResponse(OAuthConstants.ErrorInvalidRequest, "The decision parameter must be 'approve' or 'deny'."));
        }

        var consentRequest = new ConsentRequest(clientId!, responseType!, codeChallenge!, codeChallengeMethod!, state);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return await RenderConsentPageAsync(validation, consentRequest, texts => texts.Get(OAuthConsentTexts.ErrorFieldsRequired));
        }

        var issueResult = await _mediator.Send(new IssueOAuthAuthorizationCodeCommand(
            email, password, clientId!, validation.EffectiveRedirectUri!, codeChallenge!, accessMode,
            DirectLoopbackRequest.IsDirectLoopback(HttpContext)));

        if (issueResult.IsSetupRequired || issueResult.Error == OAuthConstants.ErrorAccessDenied)
        {
            return await RenderConsentPageAsync(validation, consentRequest, texts => LocalizeIssueError(texts, issueResult));
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

        return RedirectWithPageHeaders(QueryHelpers.AddQueryString(validation.EffectiveRedirectUri!, query));
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

        return RedirectWithPageHeaders(QueryHelpers.AddQueryString(redirectUri, query));
    }

    private IActionResult RedirectWithPageHeaders(string url)
    {
        OAuthConsentPageHeaders.Apply(Response);
        return Redirect(url);
    }

    private static void AppendState(Dictionary<string, string?> query, string? state)
    {
        if (!string.IsNullOrEmpty(state))
        {
            query[OAuthConstants.ParameterState] = state;
        }
    }

    private async Task<IActionResult> RenderConsentPageAsync(
        OAuthAuthorizeValidationResult validation,
        ConsentRequest consentRequest,
        Func<OAuthConsentTextSet, string?>? errorFor)
    {
        var texts = await _consentTexts.GetAsync(PreferredLanguages(), HttpContext.RequestAborted);
        var antiforgeryTokens = _antiforgery.GetAndStoreTokens(HttpContext);

        var hiddenFields = new Dictionary<string, string>
        {
            [antiforgeryTokens.FormFieldName] = antiforgeryTokens.RequestToken ?? string.Empty,
            [OAuthConstants.ParameterClientId] = consentRequest.ClientId,
            [OAuthConstants.ParameterRedirectUri] = validation.EffectiveRedirectUri!,
            [OAuthConstants.ParameterResponseType] = consentRequest.ResponseType,
            [OAuthConstants.ParameterCodeChallenge] = consentRequest.CodeChallenge,
            [OAuthConstants.ParameterCodeChallengeMethod] = consentRequest.CodeChallengeMethod,
            [OAuthConstants.ParameterState] = consentRequest.State ?? string.Empty
        };

        var model = new OAuthConsentPageModel(
            $"/{OAuthConstants.RouteBase}/{OAuthConstants.AuthorizeEndpointName}",
            validation.Match!,
            validation.IsWriteAllowed,
            validation.AccessTokenLifetime,
            texts,
            hiddenFields,
            errorFor?.Invoke(texts));

        OAuthConsentPageHeaders.Apply(Response);

        return Content(OAuthLoginPageRenderer.Render(model), MediaTypeNames.Text.Html);
    }

    private List<string> PreferredLanguages()
    {
        return Request.GetTypedHeaders().AcceptLanguage
            .Where(value => value.Value.HasValue && (value.Quality ?? 1) > 0)
            .OrderByDescending(value => value.Quality ?? 1)
            .Select(value => value.Value.Value!)
            .Take(OAuthConstants.MaxAcceptLanguageEntries)
            .ToList();
    }

    private static string LocalizeIssueError(OAuthConsentTextSet texts, OAuthAuthorizationCodeResult result)
    {
        return result.Failure switch
        {
            OAuthIssueFailure.SetupRequired => texts.Get(OAuthConsentTexts.ErrorSetupRequired),
            OAuthIssueFailure.InvalidCredentials => texts.Get(OAuthConsentTexts.ErrorInvalidCredentials),
            OAuthIssueFailure.AccountNotAllowed => texts.Get(OAuthConsentTexts.ErrorAccountNotAllowed),
            _ => result.ErrorDescription ?? texts.Get(OAuthConsentTexts.ErrorInvalidCredentials)
        };
    }

    private sealed record ConsentRequest(
        string ClientId,
        string ResponseType,
        string CodeChallenge,
        string CodeChallengeMethod,
        string? State);
}
