using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountRegistrationService : IAccountRegistrationService
{
    private readonly DataBaseContext _appDbContext;
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly ILogger<AccountRegistrationService> _logger;

    public AccountRegistrationService(
        DataBaseContext appDbContext,
        IAuthenticationService authenticationService,
        IUserManagementService userManagementService,
        IRefreshTokenService refreshTokenService,
        IAccountAuthenticationService accountAuthenticationService,
        IAccountPasswordService accountPasswordService,
        ILogger<AccountRegistrationService> logger)
    {
        _appDbContext = appDbContext;
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _refreshTokenService = refreshTokenService;
        _accountAuthenticationService = accountAuthenticationService;
        _accountPasswordService = accountPasswordService;
        this._logger = logger;
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

        try
        {
            _logger.LogInformation("Generating password reset token for new user: {Email}", user.Email);
            var tokenGenerated = await _accountPasswordService.GeneratePasswordResetTokenAsync(user.Email);
            
            if (tokenGenerated)
            {
                _logger.LogInformation("Password reset token generated and email sent for new user: {Email}", user.Email);
            }
            else
            {
                _logger.LogWarning("Failed to generate password reset token for new user: {Email}", user.Email);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for new user: {Email}", user.Email);
        }

        _logger.LogInformation("User registered successfully: {Email}", user.Email);
        return authenticatedResult;
    }
}