// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Accounts;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Domain.Security;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Klacks.Api.Domain.Services.Accounts;

/// <summary>
/// Changes, resets and issues reset tokens for account passwords. A change binds to the caller's id, refuses blocked
/// accounts before the old password is checked, revokes all refresh tokens of the account and hands back a fresh token
/// pair for the current session; personal access tokens stay. A reset revokes all refresh tokens and all personal
/// access tokens (OAuth access tokens included) of the account.
/// </summary>
/// <param name="authenticationService">Checks and changes the password and keeps the lockout counter</param>
/// <param name="userManagementService">Looks up and updates accounts</param>
/// <param name="notificationService">Sends the reset mail</param>
/// <param name="accountAuthenticationService">Issues the fresh token pair after a change</param>
/// <param name="refreshTokenService">Revokes the refresh tokens of the account</param>
/// <param name="personalAccessTokenRepository">Revokes the personal access tokens of the account after a reset</param>
/// <param name="settings">Base URL of the reset link</param>
/// <param name="logger">Records outcomes without personal data</param>
public class AccountPasswordService : IAccountPasswordService
{
    private const int ResetTokenValidityHours = 24;
    private const int ResetTokenByteLength = 64;
    private const string PasswordChangeFailedKey = "Password change failed";
    private const string PasswordChangeFailedMessage = "The password could not be changed.";

    private readonly IAuthenticationService _authenticationService;
    private readonly IUserManagementService _userManagementService;
    private readonly IAccountNotificationService _notificationService;
    private readonly IAccountAuthenticationService _accountAuthenticationService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IPersonalAccessTokenRepository _personalAccessTokenRepository;
    private readonly PasswordResetSettings _settings;
    private readonly ILogger<AccountPasswordService> _logger;

    public AccountPasswordService(
        IAuthenticationService authenticationService,
        IUserManagementService userManagementService,
        IAccountNotificationService notificationService,
        IAccountAuthenticationService accountAuthenticationService,
        IRefreshTokenService refreshTokenService,
        IPersonalAccessTokenRepository personalAccessTokenRepository,
        IOptions<PasswordResetSettings> settings,
        ILogger<AccountPasswordService> logger)
    {
        _authenticationService = authenticationService;
        _userManagementService = userManagementService;
        _notificationService = notificationService;
        _accountAuthenticationService = accountAuthenticationService;
        _refreshTokenService = refreshTokenService;
        _personalAccessTokenRepository = personalAccessTokenRepository;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<AuthenticatedResult> ChangePasswordAsync(string userId, ChangePasswordResource model)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        var existingUser = await _userManagementService.FindUserByIdAsync(userId);

        if (existingUser == null)
        {
            _logger.LogWarning("Password change requested for an unknown account");
            _authenticationService.SetModelError(authenticatedResult, PasswordChangeFailedKey, PasswordChangeFailedMessage);
            return authenticatedResult;
        }

        if (await _userManagementService.IsAccountBlockedAsync(existingUser))
        {
            _logger.LogWarning("Password change refused for blocked account {UserId}", existingUser.Id);
            _authenticationService.SetModelError(authenticatedResult, PasswordChangeFailedKey, PasswordChangeFailedMessage);
            return authenticatedResult;
        }

        var (success, result) = await _authenticationService.ChangePasswordAsync(existingUser, model.OldPassword, model.Password);
        if (success)
        {
            _logger.LogInformation("Password changed successfully for user {UserId}", existingUser.Id);
            await _refreshTokenService.RemoveAllUserRefreshTokensAsync(existingUser.Id);

            var renewedSession = await _accountAuthenticationService.GenerateAuthenticationAsync(existingUser);
            renewedSession.Email = existingUser.Email ?? string.Empty;
            return renewedSession;
        }

        if (result != null)
        {
            _logger.LogWarning("Password change failed for user {UserId}: {Errors}", existingUser.Id, string.Join(", ", result.Errors.Select(e => e.Code)));
            authenticatedResult.ModelState = _authenticationService.AddErrorsToModelState(result, authenticatedResult.ModelState);
        }
        else
        {
            _logger.LogError("Unexpected error during password change for user {UserId}", existingUser.Id);
            _authenticationService.SetModelError(authenticatedResult, "An unexpected error has occurred.", string.Empty);
        }

        return authenticatedResult;
    }

    public async Task<AuthenticatedResult> ResetPasswordAsync(ResetPasswordResource data)
    {
        var authenticatedResult = new AuthenticatedResult { Success = false };

        var existingUser = await _userManagementService.FindUserByTokenAsync(data.Token);
        if (existingUser == null || existingUser.PasswordResetToken != PasswordResetTokenHasher.Hash(data.Token))
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

            await _refreshTokenService.RemoveAllUserRefreshTokensAsync(existingUser.Id);
            var revokedAccessTokens = await _personalAccessTokenRepository.RevokeAllByUserAsync(existingUser.Id, CancellationToken.None);
            _logger.LogInformation("Password reset revoked all refresh tokens and {Count} personal access tokens of user {UserId}", revokedAccessTokens, existingUser.Id);

            _logger.LogInformation("Password reset successfully for user {UserId}", existingUser.Id);
            authenticatedResult.Success = true;
        }
        else if (result != null)
        {
            _logger.LogWarning("Password reset failed for user {UserId}: {Errors}", existingUser.Id, string.Join(", ", result.Errors.Select(e => e.Code)));
            authenticatedResult.ModelState = _authenticationService.AddErrorsToModelState(result, authenticatedResult.ModelState);
        }
        else
        {
            _logger.LogError("Unexpected error during password reset for user {UserId}", existingUser.Id);
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
                _logger.LogWarning("Password reset requested for non-existing email");
                return false;
            }

            var rawToken = GenerateSecureToken();

            user.PasswordResetToken = PasswordResetTokenHasher.Hash(rawToken);
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(ResetTokenValidityHours);
            
            await _userManagementService.UpdateUserAsync(user);

            try
            {
                var resetLink = $"{_settings.BaseUrl}/reset-password?token={rawToken}";
                var message = $@"
                    <h2>Reset Password</h2>
                    <p>You have requested to reset your password.</p>
                    <p>Click the following link to reset your password:</p>
                    <p><a href=""{resetLink}"">Reset Password</a></p>
                    <p>This link is valid for {ResetTokenValidityHours} hours.</p>
                    <p>If you did not make this request, please ignore this email.</p>";

                await _notificationService.SendEmailAsync("Reset Password", email, message);
                _logger.LogInformation("Password reset email sent for user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Password reset token generated but failed to send email for user {UserId}", user.Id);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating password reset token");
            return false;
        }
    }

    public async Task<bool> ValidatePasswordResetTokenAsync(string token)
    {
        try
        {
            var user = await _userManagementService.FindUserByTokenAsync(token);
            return user != null &&
                   user.PasswordResetToken == PasswordResetTokenHasher.Hash(token) &&
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
        var bytes = new byte[ResetTokenByteLength];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
    }
}