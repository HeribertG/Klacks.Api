// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler that authenticates the resource owner with email and password and issues a
/// short-lived single-use authorization code bound to client, redirect URI and PKCE challenge.
/// </summary>
/// <param name="request">Contains the user credentials and the validated OAuth request parameters</param>

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text.Json;
using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.OAuth;

public class IssueOAuthAuthorizationCodeCommandHandler : IRequestHandler<IssueOAuthAuthorizationCodeCommand, OAuthAuthorizationCodeResult>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOAuthAuthorizationCodeStore _codeStore;

    public IssueOAuthAuthorizationCodeCommandHandler(
        IOAuthClientRepository clientRepository,
        IAuthenticationService authenticationService,
        IOAuthAuthorizationCodeStore codeStore)
    {
        _clientRepository = clientRepository;
        _authenticationService = authenticationService;
        _codeStore = codeStore;
    }

    public async Task<OAuthAuthorizationCodeResult> Handle(IssueOAuthAuthorizationCodeCommand request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
            return OAuthAuthorizationCodeResult.Rejected(OAuthConstants.ErrorInvalidClient, "Unknown client.");
        }

        var registeredUris = JsonSerializer.Deserialize<List<string>>(client.RedirectUrisJson) ?? [];
        if (!registeredUris.Contains(request.RedirectUri, StringComparer.Ordinal))
        {
            return OAuthAuthorizationCodeResult.Rejected(
                OAuthConstants.ErrorInvalidRequest,
                "The redirect_uri is not registered for this client.");
        }

        var (isValid, user) = await _authenticationService.ValidateCredentialsAsync(request.Email, request.Password);
        if (!isValid || user == null)
        {
            return OAuthAuthorizationCodeResult.Rejected(OAuthConstants.ErrorAccessDenied, "Invalid email or password.");
        }

        var code = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(OAuthConstants.AuthorizationCodeByteLength));

        _codeStore.Store(code, new OAuthAuthorizationCodeData(
            UserId: user.Id,
            ClientId: request.ClientId,
            ClientName: client.ClientName,
            RedirectUri: request.RedirectUri,
            CodeChallenge: request.CodeChallenge,
            Scope: request.Scope));

        return OAuthAuthorizationCodeResult.Success(code);
    }
}
