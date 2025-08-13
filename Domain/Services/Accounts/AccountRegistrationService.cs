using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountRegistrationService : IAccountRegistrationService
{
    private readonly DataBaseContext _appDbContext;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly ILogger<AccountRegistrationService> _logger;

    public AccountRegistrationService(
        DataBaseContext appDbContext,
        IAuthenticationService authenticationService,
        IUserManagementService userManagementService,
        IRefreshTokenService refreshTokenService,
        IAccountAuthenticationService accountAuthenticationService,
        ILogger<AccountRegistrationService> logger)
    {
        _appDbContext = appDbContext;
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _refreshTokenService = refreshTokenService;
        _accountAuthenticationService = accountAuthenticationService;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> RegisterUserAsync(AppUser user, string password)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        var (success, result) = await _userManagementService.RegisterUserAsync(user, password);
        
        if (!success)
        {
            if (result != null)
            {
                _logger.LogWarning("User registration failed for {Email}: {Errors}", user.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                authenticatedResult.ModelState = _authenticationService.AddErrorsToModelState(result, authenticatedResult.ModelState);
            }
            else
            {
                _logger.LogError("User registration failed for {Email}: Unknown error", user.Email);
                _authenticationService.SetModelError(authenticatedResult, "Registration failed", "User registration failed. Please check your input.");
            }

            return authenticatedResult;
        }

        var expires = _refreshTokenService.CalculateTokenExpiryTime();
        authenticatedResult = await _accountAuthenticationService.SetAuthenticatedResultAsync(authenticatedResult, user, expires);

        await _appDbContext.SaveChangesAsync();

        _logger.LogInformation("User registered successfully: {Email}", user.Email);
        return authenticatedResult;
    }
}