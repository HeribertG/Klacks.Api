// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Handler that authenticates the resource owner with email and password and issues a
/// short-lived single-use authorization code bound to client, redirect URI, PKCE challenge and access mode. The redirect
/// URI is checked again here against the registration and the trust settings (the same resolution as at authorization),
/// and the code binds read access unless the user chose write and the installation allows it.
/// Refuses the seeded admin account while its own-admin setup gate is active, so no MCP token can be
/// minted for the publicly known seed credentials before setup, and refuses accounts without
/// CanManageAccessTokens (planners), because the code is exchanged for a personal access token.
/// </summary>
/// <param name="request">Contains the user credentials and the validated OAuth request parameters</param>

using System.Buffers.Text;
using System.Security.Cryptography;
using Klacks.Api.Application.Commands.OAuth;
using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Application.Interfaces.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.OAuth;

public class IssueOAuthAuthorizationCodeCommandHandler : IRequestHandler<IssueOAuthAuthorizationCodeCommand, OAuthAuthorizationCodeResult>
{
    private readonly IOAuthClientRepository _clientRepository;
    private readonly IAuthenticationService _authenticationService;
    private readonly IOAuthAuthorizationCodeStore _codeStore;
    private readonly IAdminSetupGateService _adminSetupGateService;
    private readonly IUserManagementService _userManagementService;
    private readonly IOAuthRedirectTrustPolicy _trustPolicy;

    public IssueOAuthAuthorizationCodeCommandHandler(
        IOAuthClientRepository clientRepository,
        IAuthenticationService authenticationService,
        IOAuthAuthorizationCodeStore codeStore,
        IAdminSetupGateService adminSetupGateService,
        IUserManagementService userManagementService,
        IOAuthRedirectTrustPolicy trustPolicy)
    {
        _clientRepository = clientRepository;
        _authenticationService = authenticationService;
        _codeStore = codeStore;
        _adminSetupGateService = adminSetupGateService;
        _userManagementService = userManagementService;
        _trustPolicy = trustPolicy;
    }

    public async Task<OAuthAuthorizationCodeResult> Handle(IssueOAuthAuthorizationCodeCommand request, CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetByClientIdAsync(request.ClientId, cancellationToken);
        if (client == null)
        {
            return OAuthAuthorizationCodeResult.Rejected(OAuthConstants.ErrorInvalidClient, "Unknown client.");
        }

        var resolution = await _trustPolicy.ResolveAsync(client, request.RedirectUri, cancellationToken);
        if (!resolution.IsResolved)
        {
            return OAuthAuthorizationCodeResult.Rejected(resolution.Error!, resolution.ErrorDescription!);
        }

        if (await _adminSetupGateService.IsSeedAdminSignInReservedForSetupCodeAsync(request.Email, request.IsLocalRequest))
        {
            return OAuthAuthorizationCodeResult.Rejected(
                OAuthConstants.ErrorAccessDenied,
                OAuthConstants.InvalidCredentialsDescription,
                OAuthIssueFailure.InvalidCredentials);
        }

        var (isValid, user) = await _authenticationService.ValidateCredentialsAsync(request.Email, request.Password);
        if (!isValid || user == null)
        {
            return OAuthAuthorizationCodeResult.Rejected(
                OAuthConstants.ErrorAccessDenied,
                OAuthConstants.InvalidCredentialsDescription,
                OAuthIssueFailure.InvalidCredentials);
        }

        if (user.Id == SystemAccounts.SeedAdminUserId && await _adminSetupGateService.IsGateActiveAsync())
        {
            return OAuthAuthorizationCodeResult.SetupRequired();
        }

        var roles = await _userManagementService.GetUserRolesAsync(user);
        if (!Permissions.HasPermission(Permissions.ExpandRoles(roles), Permissions.CanManageAccessTokens))
        {
            return OAuthAuthorizationCodeResult.Rejected(
                OAuthConstants.ErrorAccessDenied,
                OAuthConstants.AccountNotAllowedDescription,
                OAuthIssueFailure.AccountNotAllowed);
        }

        var code = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(OAuthConstants.AuthorizationCodeByteLength));

        var accessMode = string.Equals(request.RequestedAccessMode, OAuthConstants.AccessModeWrite, StringComparison.Ordinal)
                         && resolution.Settings.AllowWrite
            ? PersonalAccessTokenAccessMode.Write
            : PersonalAccessTokenAccessMode.Read;

        await _codeStore.StoreAsync(code, new OAuthAuthorizationCodeData(
            UserId: user.Id,
            ClientId: request.ClientId,
            RedirectUri: resolution.RedirectUri!,
            CodeChallenge: request.CodeChallenge,
            AccessMode: accessMode), cancellationToken);

        return OAuthAuthorizationCodeResult.Success(code);
    }
}
