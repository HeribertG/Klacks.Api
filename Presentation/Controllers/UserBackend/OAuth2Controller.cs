using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

[ApiController]
[Route("api/backend/[controller]")]
public class OAuth2Controller : ControllerBase
{
    private readonly IIdentityProviderRepository _providerRepository;
    private readonly IOAuth2Service _oauth2Service;
    private readonly IAccountAuthenticationService _authService;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<OAuth2Controller> _logger;

    public OAuth2Controller(
        IIdentityProviderRepository providerRepository,
        IOAuth2Service oauth2Service,
        IAccountAuthenticationService authService,
        UserManager<AppUser> userManager,
        ILogger<OAuth2Controller> logger)
    {
        _providerRepository = providerRepository;
        _oauth2Service = oauth2Service;
        _authService = authService;
        _userManager = userManager;
        _logger = logger;
    }

    [AllowAnonymous]
    [HttpGet("providers")]
    public async Task<IActionResult> GetOAuth2Providers()
    {
        var providers = await _providerRepository.GetAuthenticationProviders();
        var oauth2Providers = providers
            .Where(p => p.Type == Domain.Enums.IdentityProviderType.OAuth2 ||
                        p.Type == Domain.Enums.IdentityProviderType.OpenIdConnect)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Type
            });

        return Ok(oauth2Providers);
    }

    [AllowAnonymous]
    [HttpGet("authorize/{providerId}")]
    public async Task<IActionResult> Authorize(Guid providerId, [FromQuery] string redirectUri)
    {
        var provider = await _providerRepository.Get(providerId);
        if (provider == null)
        {
            return NotFound("Provider not found");
        }

        if (provider.Type != Domain.Enums.IdentityProviderType.OAuth2 &&
            provider.Type != Domain.Enums.IdentityProviderType.OpenIdConnect)
        {
            return BadRequest("Provider is not an OAuth2/OpenID Connect provider");
        }

        var state = $"{providerId}_{Guid.NewGuid():N}";
        var authUrl = _oauth2Service.GetAuthorizationUrl(provider, redirectUri, state);

        _logger.LogInformation("[OAUTH2] Authorization URL generated for provider {Provider}", provider.Name);

        return Ok(new { authorizationUrl = authUrl, state });
    }

    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] OAuth2CallbackRequest request)
    {
        if (string.IsNullOrEmpty(request.Code) || string.IsNullOrEmpty(request.State))
        {
            return BadRequest("Missing code or state parameter");
        }

        var stateParts = request.State.Split('_');
        if (stateParts.Length < 2 || !Guid.TryParse(stateParts[0], out var providerId))
        {
            _logger.LogWarning("[OAUTH2] Invalid state parameter format");
            return BadRequest("Invalid state parameter");
        }

        var provider = await _providerRepository.Get(providerId);
        if (provider == null)
        {
            return NotFound("Provider not found");
        }

        var tokenResponse = await _oauth2Service.ExchangeCodeForTokenAsync(provider, request.Code, request.RedirectUri);
        if (tokenResponse == null)
        {
            _logger.LogError("[OAUTH2] Failed to exchange code for token");
            return Unauthorized("Failed to exchange code for token");
        }

        var userInfo = await _oauth2Service.GetUserInfoAsync(provider, tokenResponse.AccessToken);
        if (userInfo == null)
        {
            _logger.LogError("[OAUTH2] Failed to get user info");
            return Unauthorized("Failed to get user info");
        }

        _logger.LogInformation("[OAUTH2] User authenticated: {Email}", userInfo.Email);

        var user = await GetOrCreateOAuth2UserAsync(userInfo, provider);
        if (user == null)
        {
            return Unauthorized("Failed to create or find user");
        }

        var result = await _authService.GenerateAuthenticationAsync(user);

        return Ok(new
        {
            result.Token,
            result.RefreshToken,
            result.Expires,
            result.UserName,
            result.FirstName,
            result.Name,
            result.Id,
            result.IsAdmin,
            result.IsAuthorised
        });
    }

    private async Task<AppUser?> GetOrCreateOAuth2UserAsync(OAuth2UserInfo userInfo, IdentityProvider provider)
    {
        if (string.IsNullOrEmpty(userInfo.Email))
        {
            _logger.LogError("[OAUTH2] User info does not contain email");
            return null;
        }

        var user = await _userManager.FindByEmailAsync(userInfo.Email);
        if (user != null)
        {
            return user;
        }

        var newUser = new AppUser
        {
            UserName = userInfo.Email,
            Email = userInfo.Email,
            EmailConfirmed = true,
            FirstName = userInfo.GivenName ?? userInfo.Name ?? string.Empty,
            LastName = userInfo.FamilyName ?? string.Empty
        };

        var result = await _userManager.CreateAsync(newUser);
        if (result.Succeeded)
        {
            _logger.LogInformation("[OAUTH2] Created new user: {Email} via provider {Provider}", userInfo.Email, provider.Name);
            return newUser;
        }

        _logger.LogError("[OAUTH2] Failed to create user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        return null;
    }
}

public class OAuth2CallbackRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
}
