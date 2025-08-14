using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Presentation.DTOs.Registrations;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountPasswordService : IAccountPasswordService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<AccountPasswordService> _logger;

    public AccountPasswordService(
        IAuthenticationService authenticationService,
        IUserManagementService userManagementService,
        ILogger<AccountPasswordService> logger)
    {
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> ChangePasswordAsync(ChangePasswordResource model)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        var existingUser = await _userManagementService.FindUserByEmailAsync(model.Email);

        if (existingUser == null)
        {
            _authenticationService.SetModelError(authenticatedResult, "The user does not exist", "There is no user with this e-mail address.");
            return authenticatedResult;
        }

        var (success, result) = await _authenticationService.ChangePasswordAsync(existingUser, model.OldPassword, model.Password);
        if (success)
        {
            _logger.LogInformation("Password changed successfully for user {Email}", model.Email);
            authenticatedResult.Success = true;
        }
        else if (result != null)
        {
            _logger.LogWarning("Password change failed for user {Email}: {Errors}", model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            authenticatedResult.ModelState = _authenticationService.AddErrorsToModelState(result, authenticatedResult.ModelState);
        }
        else
        {
            _logger.LogError("Unexpected error during password change for user {Email}", model.Email);
            _authenticationService.SetModelError(authenticatedResult, "An unexpected error has occurred.", string.Empty);
        }

        return authenticatedResult;
    }

    public Task<AuthenticatedResult> ResetPasswordAsync(ResetPasswordResource data)
    {
        _logger.LogWarning("ResetPassword not yet implemented");
        throw new NotImplementedException("Password reset functionality is not yet implemented.");
    }
}