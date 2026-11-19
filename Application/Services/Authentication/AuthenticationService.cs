// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Identity;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.Validation.Accounts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Klacks.Api.Application.Services.Authentication;

/// <summary>
/// Checks sign-in credentials: first the local password of the account with the typed e-mail, then each enabled
/// LDAP/AD authentication provider in SortOrder by search-then-bind. A directory sign-in maps only onto the account an
/// administrator linked to that directory entry; a correct directory password without such an active account ends in
/// DirectoryAccessNotGrantedException. For a deactivated account the local password check (and its lockout counter) is
/// skipped; the directory loop then refuses it, because it grants only active linked accounts. An entry linked to the
/// seeded admin account is refused, and a failed attempt already counted by the local check is not counted twice.
/// </summary>
/// <param name="userManager">Identity store: local password, lockout counter</param>
/// <param name="jwtValidator">Validates access tokens</param>
/// <param name="identityProviderRepository">Lists the authentication providers</param>
/// <param name="ldapService">Finds the directory entry and binds as it</param>
/// <param name="userReader">Finds the account linked to a directory entry</param>
/// <param name="logger">Records outcomes without credentials</param>
public class AuthenticationService : IAuthenticationService
{
    private readonly UserManager<AppUser> _userManager;
    private readonly JwtValidator _jwtValidator;
    private readonly IIdentityProviderRepository _identityProviderRepository;
    private readonly ILdapService _ldapService;
    private readonly IAppUserReader _userReader;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        UserManager<AppUser> userManager,
        JwtValidator jwtValidator,
        IIdentityProviderRepository identityProviderRepository,
        ILdapService ldapService,
        IAppUserReader userReader,
        ILogger<AuthenticationService> logger)
    {
        _userManager = userManager;
        _jwtValidator = jwtValidator;
        _identityProviderRepository = identityProviderRepository;
        _ldapService = ldapService;
        _userReader = userReader;
        _logger = logger;
    }

    public async Task<(bool IsValid, AppUser? User)> ValidateCredentialsAsync(string email, string password)
    {
        _logger.LogInformation("ValidateCredentialsAsync called");

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _logger.LogWarning("Empty email or password");
            return (false, null);
        }

        var user = await _userManager.FindByEmailAsync(email);

        var isDeactivated = user is { DeactivatedAt: not null };
        if (isDeactivated)
        {
            _logger.LogWarning("Account is deactivated for user {UserId}; local password check skipped", user!.Id);
        }

        string? locallyCountedUserId = null;
        if (user != null && !isDeactivated && await _userManager.HasPasswordAsync(user))
        {
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Account is locked out for user {UserId}", user.Id);
                return (false, null);
            }

            if (await _userManager.CheckPasswordAsync(user, password))
            {
                await _userManager.ResetAccessFailedCountAsync(user);
                _logger.LogInformation("Local authentication successful for user {UserId}", user.Id);
                return (true, user);
            }

            await _userManager.AccessFailedAsync(user);
            locallyCountedUserId = user.Id;
            _logger.LogWarning("Local authentication failed (invalid password) for user {UserId}", user.Id);
        }

        _logger.LogInformation("Local authentication failed, trying directory providers");
        var directoryResult = await ValidateDirectoryCredentialsAsync(email, password, locallyCountedUserId);
        if (directoryResult.IsValid)
        {
            _logger.LogInformation("Directory authentication successful for user {UserId}", directoryResult.User!.Id);
            return directoryResult;
        }

        _logger.LogWarning("All authentication methods failed");
        return (false, null);
    }

    private async Task<(bool IsValid, AppUser? User)> ValidateDirectoryCredentialsAsync(
        string loginName, string password, string? locallyCountedUserId)
    {
        var allProviders = await _identityProviderRepository.GetAuthenticationProviders();
        var providers = allProviders.Where(DirectoryLoginProviders.IsUsableForLogin).ToList();
        _logger.LogInformation("Found {Count} LDAP/AD authentication providers (of {Total} total)", providers.Count, allProviders.Count);

        var notGranted = false;

        foreach (var provider in providers)
        {
            try
            {
                var outcome = await TryDirectoryProviderAsync(provider, loginName, password, locallyCountedUserId);
                switch (outcome.Kind)
                {
                    case DirectorySignInOutcome.Granted:
                        return (true, outcome.User);
                    case DirectorySignInOutcome.Refused:
                        return (false, null);
                    case DirectorySignInOutcome.NotGranted:
                        notGranted = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Directory authentication failed for provider {ProviderId}", provider.Id);
            }
        }

        if (notGranted)
        {
            throw new DirectoryAccessNotGrantedException();
        }

        return (false, null);
    }

    private async Task<(DirectorySignInOutcome Kind, AppUser? User)> TryDirectoryProviderAsync(
        IdentityProvider provider, string loginName, string password, string? locallyCountedUserId)
    {
        var entry = await _ldapService.FindLoginEntryAsync(provider, loginName);
        if (entry == null)
        {
            return (DirectorySignInOutcome.NoMatch, null);
        }

        var linked = await _userReader.FindByDirectoryLinkAsync(provider.Id, entry.StableId);
        if (linked?.Id == SystemAccounts.SeedAdminUserId)
        {
            _logger.LogWarning("Directory sign-in refused: the entry is linked to the seeded admin account");
            return (DirectorySignInOutcome.Refused, null);
        }

        var linkedActive = linked is { DeactivatedAt: null };

        if (linkedActive && await _userManager.IsLockedOutAsync(linked!))
        {
            _logger.LogWarning("Account is locked out for user {UserId}", linked!.Id);
            return (DirectorySignInOutcome.Refused, null);
        }

        if (!await _ldapService.BindAsUserAsync(provider, entry.DistinguishedName, password))
        {
            if (linkedActive && linked!.Id != locallyCountedUserId)
            {
                await _userManager.AccessFailedAsync(linked);
                _logger.LogWarning("Directory authentication failed (invalid password) for user {UserId}", linked!.Id);
            }

            return (DirectorySignInOutcome.NoMatch, null);
        }

        if (!linkedActive)
        {
            _logger.LogWarning(
                "Directory bind succeeded via provider {ProviderId} but the entry is not granted access (no active linked account)",
                provider.Id);
            return (DirectorySignInOutcome.NotGranted, null);
        }

        await _userManager.ResetAccessFailedCountAsync(linked!);
        return (DirectorySignInOutcome.Granted, linked);
    }

    /// <summary>
    /// Changes the password and feeds the lockout counter like a login: a wrong old password counts as a failed access,
    /// a successful change resets the counter. A new password that only fails the password policy is not counted.
    /// The caller checks beforehand that the account is neither locked out nor deactivated.
    /// </summary>
    /// <param name="user">Account whose password changes</param>
    /// <param name="oldPassword">Current password as typed by the caller</param>
    /// <param name="newPassword">Requested new password</param>
    public async Task<(bool Success, IdentityResult? Result)> ChangePasswordAsync(AppUser user, string oldPassword, string newPassword)
    {
        try
        {
            var result = await _userManager.ChangePasswordAsync(user, oldPassword, newPassword);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);
            }
            else if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch)))
            {
                await _userManager.AccessFailedAsync(user);
                _logger.LogWarning("Password change failed (invalid old password) for user {UserId}", user.Id);
            }

            return (result.Succeeded, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password change failed unexpectedly for user {UserId}", user.Id);
            return (false, null);
        }
    }

    public async Task<AppUser?> GetUserFromAccessTokenAsync(string token)
    {
        try
        {
            var principal = _jwtValidator.ValidateToken(token);
            if (principal == null)
            {
                return null;
            }

            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return null;
            }

            return await _userManager.FindByIdAsync(userIdClaim.Value);
        }
        catch (SecurityTokenException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resolving the user from the access token failed unexpectedly");
            return null;
        }
    }

    public ModelStateDictionary AddErrorsToModelState(IdentityResult identityResult, ModelStateDictionary? modelState = null)
    {
        modelState ??= new ModelStateDictionary();

        foreach (var error in identityResult.Errors)
        {
            modelState.TryAddModelError(error.Code, error.Description);
        }

        return modelState;
    }

    public void SetModelError(AuthenticatedResult model, string key, string message)
    {
        model.ModelState ??= new ModelStateDictionary();
        model.ModelState.TryAddModelError(key, message);
    }

    public async Task<(bool Success, IdentityResult? Result)> ResetPasswordAsync(AppUser user, string token, string newPassword)
    {
        try
        {
            var passwordHasher = new PasswordHasher<AppUser>();
            user.PasswordHash = passwordHasher.HashPassword(user, newPassword);
            user.AccessFailedCount = 0;
            user.LockoutEnd = null;

            var result = await _userManager.UpdateAsync(user);
            return (result.Succeeded, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Password reset failed unexpectedly for user {UserId}", user.Id);
            return (false, null);
        }
    }
}