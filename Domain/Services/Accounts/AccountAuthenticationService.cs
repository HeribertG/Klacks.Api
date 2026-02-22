using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.Registrations;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountAuthenticationService : IAccountAuthenticationService
{
    private readonly ITokenService _tokenService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<AccountAuthenticationService> _logger;

    public AccountAuthenticationService(
        ITokenService tokenService,
        IAuthenticationService authenticationService,
        IUserManagementService userManagementService,
        IRefreshTokenService refreshTokenService,
        ILogger<AccountAuthenticationService> logger)
    {
        _tokenService = tokenService;
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> LogInUserAsync(string email, string password)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };
        
        var (isValid, user) = await _authenticationService.ValidateCredentialsAsync(email, password);
        
        if (!isValid || user == null)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _authenticationService.SetModelError(authenticatedResult, "Login failed", "The e-mail and password must not be empty.");
            }
            else
            {
                _authenticationService.SetModelError(authenticatedResult, "Login failed", "The login details are not correct.");
            }

            return authenticatedResult;
        }

        try
        {
            return await GenerateAuthenticationAsync(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login authentication for user {Email}", email);
            _authenticationService.SetModelError(authenticatedResult, "Login failed", ex.Message);
            return authenticatedResult;
        }
    }

    public async Task<AuthenticatedResult> RefreshTokenAsync(RefreshRequestResource model)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        if (model == null)
        {
            _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "Refresh request model cannot be null.");
            return authenticatedResult;
        }

        var user = await _authenticationService.GetUserFromAccessTokenAsync(model.Token);

        if (user == null)
        {
            user = await _refreshTokenService.GetUserFromRefreshTokenAsync(model.RefreshToken);
        }

        if (user == null)
        {
            _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "Token invalid or expired.");
            return authenticatedResult;
        }

        try
        {
            if (await _refreshTokenService.ValidateRefreshTokenAsync(user.Id, model.RefreshToken))
            {
                return await GenerateAuthenticationAsync(user, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh for user {UserId}", user.Id);
            _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "An error occurred while refreshing the token.");
            return authenticatedResult;
        }

        _authenticationService.SetModelError(authenticatedResult, "RefreshTokenError", "Invalid refresh token.");
        return authenticatedResult;
    }

    public async Task<AuthenticatedResult> GenerateAuthenticationAsync(AppUser user, bool withRefreshToken = true)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };
        var expires = _refreshTokenService.CalculateTokenExpiryTime();

        if (withRefreshToken)
        {
            var refreshToken = await _refreshTokenService.CreateRefreshTokenAsync(user.Id);
            authenticatedResult.RefreshToken = refreshToken;
        }

        authenticatedResult = await SetAuthenticatedResultAsync(authenticatedResult, user, expires);

        return authenticatedResult;
    }

    public async Task<AuthenticatedResult> SetAuthenticatedResultAsync(AuthenticatedResult authenticatedResult, AppUser user, DateTime expires)
    {
        authenticatedResult.Token = await _tokenService.CreateToken(user, expires);
        authenticatedResult.Success = true;
        authenticatedResult.Expires = expires;
        authenticatedResult.UserName = user.UserName!;
        authenticatedResult.FirstName = user.FirstName;
        authenticatedResult.Name = user.LastName;
        authenticatedResult.Id = user.Id;
        authenticatedResult.IsAdmin = await _userManagementService.IsUserInRoleAsync(user, Roles.Admin);
        authenticatedResult.IsAuthorised = await _userManagementService.IsUserInRoleAsync(user, Roles.Authorised);

        return authenticatedResult;
    }

    public async Task<bool> ValidateRefreshTokenAsync(AppUser user, string refreshToken)
    {
        return await _refreshTokenService.ValidateRefreshTokenAsync(user.Id, refreshToken);
    }

    public void SetModelErrorAsync(AuthenticatedResult model, string key, string message)
    {
        _authenticationService.SetModelError(model, key, message);
    }
}