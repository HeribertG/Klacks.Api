using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Registrations;

namespace Klacks.Api.Infrastructure.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly IAccountPasswordService _accountPasswordService;
    private readonly IAccountRegistrationService _accountRegistrationService;
    private readonly IAccountManagementService _accountManagementService;
    private readonly IAccountNotificationService _accountNotificationService;

    public AccountRepository(
        IAccountAuthenticationService accountAuthenticationService,
        IAccountPasswordService accountPasswordService,
        IAccountRegistrationService accountRegistrationService,
        IAccountManagementService accountManagementService,
        IAccountNotificationService accountNotificationService)
    {
        _accountAuthenticationService = accountAuthenticationService;
        _accountPasswordService = accountPasswordService;
        _accountRegistrationService = accountRegistrationService;
        _accountManagementService = accountManagementService;
        _accountNotificationService = accountNotificationService;
    }

    public async Task<AuthenticatedResult> ChangePassword(ChangePasswordResource model)
    {
        return await ChangePasswordUser(model);
    }

    public async Task<AuthenticatedResult> ChangePasswordUser(ChangePasswordResource model)
    {
        return await _accountPasswordService.ChangePasswordAsync(model);
    }

    public async Task<HttpResultResource> ChangeRoleUser(ChangeRole editUserRole)
    {
        return await _accountManagementService.ChangeRoleUserAsync(editUserRole);
    }

    public async Task<HttpResultResource> DeleteAccountUser(Guid id)
    {
        return await _accountManagementService.DeleteAccountUserAsync(id);
    }

    public async Task<List<UserResource>> GetUserList()
    {
        return await _accountManagementService.GetUserListAsync();
    }

    public async Task<AuthenticatedResult> LogInUser(string email, string password)
    {
        return await _accountAuthenticationService.LogInUserAsync(email, password);
    }

    public async Task<AuthenticatedResult> RefreshToken(RefreshRequestResource model)
    {
        return await _accountAuthenticationService.RefreshTokenAsync(model);
    }

    public async Task<AuthenticatedResult> RegisterUser(AppUser user, string password)
    {
        return await _accountRegistrationService.RegisterUserAsync(user, password);
    }

    public Task<AuthenticatedResult> ResetPassword(ResetPasswordResource data)
    {
        throw new NotImplementedException();
    }

    public Task<string> SendEmail(string title, string email, string message)
    {
        return _accountNotificationService.SendEmailAsync(title, email, message);
    }

    public void SetModelError(AuthenticatedResult model, string key, string message)
    {
        _accountAuthenticationService.SetModelErrorAsync(model, key, message);
    }

    public async Task<bool> ValidateRefreshTokenAsync(AppUser user, string refreshToken)
    {
        return await _accountAuthenticationService.ValidateRefreshTokenAsync(user, refreshToken);
    }


}
