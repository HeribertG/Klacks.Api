using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.Registrations;
using System.Security.Cryptography;

namespace Klacks.Api.Domain.Services.Accounts;

public class AccountPasswordService : IAccountPasswordService
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AccountPasswordService> _logger;

    public AccountPasswordService(
        IAuthenticationService authenticationService,
        IUserManagementService userManagementService,
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<AccountPasswordService> logger)
    {
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        this._logger = logger;
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

    public async Task<AuthenticatedResult> ResetPasswordAsync(ResetPasswordResource data)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        var existingUser = await _userManagementService.FindUserByTokenAsync(data.Token);
        if (existingUser == null || existingUser.PasswordResetToken != data.Token)
        {
            _logger.LogWarning("Invalid password reset token attempted");
            _authenticationService.SetModelError(authenticatedResult, "Invalid token", "The password reset token is invalid.");
            return authenticatedResult;
        }

        if (existingUser.PasswordResetTokenExpires == null || existingUser.PasswordResetTokenExpires < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired password reset token attempted for user {UserId}", existingUser.Id);
            _authenticationService.SetModelError(authenticatedResult, "Token expired", "The password reset token has expired.");
            return authenticatedResult;
        }

        var (success, result) = await _authenticationService.ResetPasswordAsync(existingUser, data.Token, data.Password);
        if (success)
        {
            existingUser.PasswordResetToken = null;
            existingUser.PasswordResetTokenExpires = null;
            await _userManagementService.UpdateUserAsync(existingUser);

            _logger.LogInformation("Password reset successfully for user {Email}", existingUser.Email);
            authenticatedResult.Success = true;
        }
        else if (result != null)
        {
            _logger.LogWarning("Password reset failed for user {Email}: {Errors}", existingUser.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
            authenticatedResult.ModelState = _authenticationService.AddErrorsToModelState(result, authenticatedResult.ModelState);
        }
        else
        {
            _logger.LogError("Unexpected error during password reset for user {Email}", existingUser.Email);
            _authenticationService.SetModelError(authenticatedResult, "An unexpected error has occurred.", string.Empty);
        }

        return authenticatedResult;
    }

    public async Task<bool> GeneratePasswordResetTokenAsync(string email)
    {
        try
        {
            var user = await _userManagementService.FindUserByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Password reset requested for non-existing email: {Email}", email);
                return false;
            }

            var token = GenerateSecureToken();
            _logger.LogInformation("Generated token for user {Email}: {Token}", email, token.Substring(0, 10) + "...");
            
            user.PasswordResetToken = token;
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(24);
            
            _logger.LogInformation("Updating user {Email} with reset token", email);
            await _userManagementService.UpdateUserAsync(user);
            _logger.LogInformation("User {Email} updated successfully with reset token", email);

            try
            {
                var notificationService = _serviceProvider.GetService<IAccountNotificationService>();
                if (notificationService != null)
                {
                    var baseUrl = _configuration["PasswordReset:BaseUrl"] ?? "https://localhost:7002";
                    var resetLink = $"{baseUrl}/reset-password?token={token}";
                    _logger.LogInformation("Generated reset link: {ResetLink}", resetLink);
                    var message = $@"
                        <h2>Reset Password</h2>
                        <p>You have requested to reset your password.</p>
                        <p>Click the following link to reset your password:</p>
                        <p><a href=""{resetLink}"">Reset Password</a></p>
                        <p>This link is valid for 24 hours.</p>
                        <p>If you did not make this request, please ignore this email.</p>";

                    await notificationService.SendEmailAsync("Reset Password", email, message);
                    _logger.LogInformation("Password reset token generated and email sent to {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Password reset token generated but notification service not available to send email to {Email}", email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password reset token generated but failed to send email to {Email}", email);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token for email: {Email}", email);
            return false;
        }
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token)
    {
        try
        {
            var user = await _userManagementService.FindUserByTokenAsync(token);
            return user != null && 
                   user.PasswordResetToken == token && 
                   user.PasswordResetTokenExpires != null && 
                   user.PasswordResetTokenExpires > DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating password reset token");
            return false;
        }
    }

    private static string GenerateSecureToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[64];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}