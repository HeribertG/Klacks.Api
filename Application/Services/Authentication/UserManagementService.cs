// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.Security;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Authentication;

/// <summary>
/// Creates, lists and deactivates application users. Deactivating is the only removal path (Owner
/// decision, 17.08.: hard delete was removed system-wide): the account keeps its data but is barred
/// from signing in, from lending its permissions, and from appearing in GetUserListAsync - a soft
/// delete, reversible only via ReactivateUserAsync.
/// </summary>
/// <param name="userManager">ASP.NET Identity store for AppUser.</param>
/// <param name="logger">Records deactivation/reactivation outcomes.</param>
public class UserManagementService : IUserManagementService
{
    private const string NotApplicable = "N/A";
    private const string UserNotFoundMessage = "User was not found.";
    private const string UserDeactivatedMessage = "User deactivated successfully.";
    private const string UserAlreadyDeactivatedMessage = "User is already deactivated.";
    private const string UserReactivatedMessage = "User reactivated successfully.";
    private const string UserNotDeactivatedMessage = "User is not deactivated.";
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UserManagementService> _logger;

    public UserManagementService(
        UserManager<AppUser> userManager,
        ILogger<UserManagementService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<(bool Success, IdentityResult? Result)> RegisterUserAsync(AppUser user, string password)
    {
        if (string.IsNullOrWhiteSpace(user.Email) || string.IsNullOrWhiteSpace(user.UserName))
        {
            return (false, null);
        }

        var existingUser = await _userManager.FindByEmailAsync(user.Email);
        if (existingUser != null)
        {
            return (false, null);
        }

        user.UserName = FormatHelper.ReplaceUmlaud(user.UserName);
        var result = await _userManager.CreateAsync(user, password);

        return (result.Succeeded, result);
    }

    public async Task<AppUser?> FindUserByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<AppUser?> FindUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<(bool Success, string Message)> ChangeUserRoleAsync(string userId, string roleName, bool isSelected)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return (false, UserNotFoundMessage);
        }

        IdentityResult? result = null;

        if (isSelected && !(await _userManager.IsInRoleAsync(user, roleName)))
        {
            result = await _userManager.AddToRoleAsync(user, roleName);
        }
        else if (!isSelected && await _userManager.IsInRoleAsync(user, roleName))
        {
            result = await _userManager.RemoveFromRoleAsync(user, roleName);
        }
        else
        {
            return (true, "No change to the role required.");
        }

        if (result == null || result.Succeeded)
        {
            return (true, "Role changed successfully.");
        }

        var errorMessage = string.Join(Environment.NewLine, result.Errors.Select(e => e.Description));
        return (false, errorMessage);
    }

    /// <summary>
    /// Stamps the two deactivation fields. Deliberately not LockoutEnd: lockout is the temporary
    /// consequence of failed sign-in attempts and clears itself, whereas this is a decision that only
    /// an administrator undoes. Nothing else about the account is touched, so reactivation is a pure
    /// reversal and no data is lost — that is what makes this the everyday alternative to deletion.
    /// </summary>
    /// <param name="userId">AppUser identifier of the account being deactivated.</param>
    /// <param name="deactivatedBy">AppUser identifier of the administrator performing the deactivation.</param>
    public async Task<(bool Success, string Message)> DeactivateUserAsync(Guid userId, string deactivatedBy)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, UserNotFoundMessage);
        }

        if (user.DeactivatedAt is not null)
        {
            return (false, UserAlreadyDeactivatedMessage);
        }

        user.DeactivatedAt = DateTime.UtcNow;
        user.DeactivatedBy = deactivatedBy;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Account {UserId} was deactivated by {DeactivatedBy}", userId, deactivatedBy);

        return (true, UserDeactivatedMessage);
    }

    /// <summary>
    /// Clears both deactivation fields. DeactivatedBy is cleared together with DeactivatedAt so no
    /// stale attribution survives into the next deactivation.
    /// </summary>
    /// <param name="userId">AppUser identifier of the account being reactivated.</param>
    public async Task<(bool Success, string Message)> ReactivateUserAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null)
        {
            return (false, UserNotFoundMessage);
        }

        if (user.DeactivatedAt is null)
        {
            return (false, UserNotDeactivatedMessage);
        }

        user.DeactivatedAt = null;
        user.DeactivatedBy = null;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            return (false, string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));
        }

        _logger.LogInformation("Account {UserId} was reactivated", userId);

        return (true, UserReactivatedMessage);
    }

    /// <summary>
    /// Single place that answers "may this account hold a session right now?", covering both the
    /// deliberate deactivation and the automatic lockout. Sign-in already answers this through the
    /// credential check; the callers that resume a session — refresh token, personal access token —
    /// never reach that check and would otherwise keep a barred account working indefinitely.
    /// </summary>
    /// <param name="user">The account whose current state is examined.</param>
    public async Task<bool> IsAccountBlockedAsync(AppUser user)
    {
        if (user.DeactivatedAt is not null)
        {
            return true;
        }

        return await _userManager.IsLockedOutAsync(user);
    }

    /// <summary>
    /// Lists every account that has not been deactivated. Deactivation is this system's soft-delete
    /// alternative to the removed hard-delete path (Owner decision, 17.08.): a deactivated account
    /// disappears from this list exactly as a soft-deleted row disappears from a query filter,
    /// everywhere this method is the source (user administration, Klacksy's user-listing skills).
    /// </summary>
    public async Task<List<UserResource>> GetUserListAsync()
    {
        var authorisedIds = (await _userManager.GetUsersInRoleAsync(Roles.Authorised)).Select(u => u.Id).ToHashSet();
        var adminIds = (await _userManager.GetUsersInRoleAsync(Roles.Admin)).Select(u => u.Id).ToHashSet();
        var users = await _userManager.Users
            .Where(u => u.DeactivatedAt == null)
            .OrderBy(u => u.DisplayOrder)
            .ToListAsync();
        var userResources = new List<UserResource>(users.Count);

        foreach (var user in users)
        {
            var userResource = new UserResource
            {
                Id = user.Id,
                UserName = user.UserName ?? NotApplicable,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? NotApplicable,
                IsAuthorised = authorisedIds.Contains(user.Id),
                IsAdmin = adminIds.Contains(user.Id),
                DeactivatedAt = user.DeactivatedAt,
                PhoneNumber = user.PhoneNumber,
            };

            userResources.Add(userResource);
        }

        return userResources;
    }

    public async Task<bool> IsUserInRoleAsync(AppUser user, string role)
    {
        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task<(bool Success, IdentityResult? Result)> UpdateUserAsync(AppUser user)
    {
        var result = await _userManager.UpdateAsync(user);
        return (result.Succeeded, result);
    }

    public async Task<AppUser?> FindUserByTokenAsync(string token)
    {
        var tokenHash = PasswordResetTokenHasher.Hash(token);
        return await _userManager.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == tokenHash);
    }

    public async Task<AppUser?> FindUserByIdNoTrackingAsync(string userId)
    {
        return await _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<IList<string>> GetUserRolesAsync(AppUser user)
    {
        return await _userManager.GetRolesAsync(user);
    }
}
