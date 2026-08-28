// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.OAuth2;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Logging;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Application.Handlers.OAuth2;

public class OAuth2CallbackCommandHandler : IRequestHandler<OAuth2CallbackCommand, TokenResource>
{
    private const string E2ETestCode = "E2E_TEST_CODE_FOR_OAUTH2";

    private readonly IIdentityProviderRepository _providerRepository;
    private readonly IOAuth2Service _oauth2Service;
    private readonly IOAuth2StateStore _stateStore;
    private readonly IAccountAuthenticationService _authService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IUsernameGeneratorService _usernameGenerator;
    private readonly ILogger<OAuth2CallbackCommandHandler> _logger;
    private readonly IWebHostEnvironment _environment;

    public OAuth2CallbackCommandHandler(
        IIdentityProviderRepository providerRepository,
        IOAuth2Service oauth2Service,
        IOAuth2StateStore stateStore,
        IAccountAuthenticationService authService,
        UserManager<AppUser> userManager,
        IUsernameGeneratorService usernameGenerator,
        ILogger<OAuth2CallbackCommandHandler> logger,
        IWebHostEnvironment environment)
    {
        _providerRepository = providerRepository;
        _oauth2Service = oauth2Service;
        _stateStore = stateStore;
        _authService = authService;
        _userManager = userManager;
        _usernameGenerator = usernameGenerator;
        _logger = logger;
        _environment = environment;
    }

    public async Task<TokenResource> Handle(OAuth2CallbackCommand command, CancellationToken cancellationToken)
    {
        var request = command.Request;

        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.State))
        {
            throw new BadRequestException("Missing code or state parameter");
        }

        var stateParts = request.State.Split('_');
        if (stateParts.Length < 2 || !Guid.TryParse(stateParts[0], out var providerId))
        {
            _logger.LogWarning("[OAUTH2] Invalid state parameter format");
            throw new BadRequestException("Invalid state parameter");
        }

        if (!await _stateStore.ValidateAndConsumeAsync(request.State, cancellationToken))
        {
            _logger.LogWarning("[OAUTH2] State parameter rejected: unknown, expired, or already used");
            throw new BadRequestException("Invalid or expired state parameter");
        }

        var provider = await _providerRepository.Get(providerId);
        if (provider == null)
        {
            throw new NotFoundException("Provider not found");
        }

        if (request.Code == E2ETestCode && _environment.IsDevelopment())
        {
            return await HandleE2ETestCallback(provider);
        }

        var tokenResponse = await _oauth2Service.ExchangeCodeForTokenAsync(provider, request.Code, request.RedirectUri);
        if (tokenResponse == null)
        {
            _logger.LogError("[OAUTH2] Failed to exchange code for token");
            throw new UnauthorizedException("Failed to exchange code for token");
        }

        var userInfo = await _oauth2Service.GetUserInfoAsync(provider, tokenResponse.AccessToken);
        if (userInfo == null)
        {
            _logger.LogError("[OAUTH2] Failed to get user info");
            throw new UnauthorizedException("Failed to get user info");
        }

        _logger.LogInformation("[OAUTH2] User authenticated: {Email}", userInfo.Email.MaskEmail());

        var user = await FindLocalUserAsync(userInfo);
        if (user == null)
        {
            throw new UnauthorizedException("No local account is mapped to this identity");
        }

        var result = await _authService.GenerateAuthenticationAsync(user);

        return new TokenResource
        {
            Success = true,
            Token = result.Token,
            RefreshToken = result.RefreshToken ?? string.Empty,
            Username = result.UserName ?? string.Empty,
            FirstName = result.FirstName ?? string.Empty,
            Name = result.Name ?? string.Empty,
            Id = result.Id ?? string.Empty,
            ExpTime = result.Expires,
            IsAdmin = result.IsAdmin,
            IsAuthorised = result.IsAuthorised,
            Version = MyVersion.Get()
        };
    }

    private async Task<TokenResource> HandleE2ETestCallback(IdentityProvider provider)
    {
        _logger.LogInformation("[OAUTH2] E2E Test callback for provider {Provider}", provider.Name);

        var testUserInfo = new OAuth2UserInfo
        {
            Id = "e2e-test-user",
            Email = "e2e-oauth2-test@klacks.local",
            Name = "E2E OAuth2 Test User",
            GivenName = "E2E",
            FamilyName = "TestUser"
        };

        var user = await GetOrCreateE2ETestUserAsync(testUserInfo, provider);
        if (user == null)
        {
            throw new UnauthorizedException("Failed to create E2E test user");
        }

        var result = await _authService.GenerateAuthenticationAsync(user);

        return new TokenResource
        {
            Success = true,
            Token = result.Token,
            RefreshToken = result.RefreshToken ?? string.Empty,
            Username = result.UserName ?? string.Empty,
            FirstName = result.FirstName ?? string.Empty,
            Name = result.Name ?? string.Empty,
            Id = result.Id ?? string.Empty,
            ExpTime = result.Expires,
            IsAdmin = result.IsAdmin,
            IsAuthorised = result.IsAuthorised,
            Version = MyVersion.Get()
        };
    }

    // Fail-closed: a successful external OIDC sign-in maps only onto a local account that already
    // exists. Just-in-time provisioning is deliberately disabled so an unknown external principal
    // can never mint a local login. The only creation path is the dev-only E2E helper below.
    private async Task<AppUser?> FindLocalUserAsync(OAuth2UserInfo userInfo)
    {
        if (string.IsNullOrEmpty(userInfo.Email))
        {
            _logger.LogError("[OAUTH2] User info does not contain email");
            return null;
        }

        var user = await _userManager.FindByEmailAsync(userInfo.Email);
        if (user == null)
        {
            _logger.LogWarning(
                "[OAUTH2] External identity authenticated but no local account is mapped to {Email}; just-in-time provisioning is disabled, the account must be provisioned in advance",
                userInfo.Email.MaskEmail());
        }

        return user;
    }

    // Reachable only from HandleE2ETestCallback, which itself runs only when the request carries the
    // hardcoded E2E test code AND the environment is Development. This is the sole account-creation
    // path and must never be called from the real OAuth2 callback.
    private async Task<AppUser?> GetOrCreateE2ETestUserAsync(OAuth2UserInfo userInfo, IdentityProvider provider)
    {
        if (string.IsNullOrEmpty(userInfo.Email))
        {
            _logger.LogError("[OAUTH2] E2E test user info does not contain email");
            return null;
        }

        var user = await _userManager.FindByEmailAsync(userInfo.Email);
        if (user != null)
        {
            return user;
        }

        var firstName = userInfo.GivenName ?? userInfo.Name ?? provider.Name ?? string.Empty;
        var lastName = userInfo.FamilyName ?? string.Empty;
        var generatedUsername = await _usernameGenerator.GenerateUniqueUsernameAsync(firstName, lastName);

        var newUser = new AppUser
        {
            UserName = generatedUsername,
            Email = userInfo.Email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName
        };

        var createResult = await _userManager.CreateAsync(newUser);
        if (createResult.Succeeded)
        {
            _logger.LogInformation("[OAUTH2] Created E2E test user: {Email} via provider {Provider}", userInfo.Email.MaskEmail(), provider.Name);
            return newUser;
        }

        _logger.LogError("[OAUTH2] Failed to create E2E test user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
        return null;
    }
}
