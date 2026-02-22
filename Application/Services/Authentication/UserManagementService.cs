// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Helpers;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs;
using Klacks.Api.Application.DTOs.Registrations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Authentication;

public class UserManagementService : IUserManagementService
{
    private const string NotApplicable = "N/A";
    private readonly UserManager<AppUser> _userManager;

    public UserManagementService(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
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
            return (false, "User was not found.");
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

    public async Task<(bool Success, string Message)> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return (false, "User was not found.");
            }

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded
                ? (true, "User deleted successfully.")
                : (false, string.Join(Environment.NewLine, result.Errors.Select(e => e.Description)));
        }
        catch (Exception e)
        {
            return (false, e.Message);
        }
    }

    public async Task<List<UserResource>> GetUserListAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        var userResources = new List<UserResource>(users.Count);
        var usersInAuthorisedRole = await _userManager.GetUsersInRoleAsync(Roles.Authorised);
        var usersInAdminRole = await _userManager.GetUsersInRoleAsync(Roles.Admin);

        foreach (var user in users)
        {
            var userResource = new UserResource
            {
                Id = user.Id,
                UserName = user.UserName ?? NotApplicable,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? NotApplicable,
                IsAuthorised = usersInAuthorisedRole.Contains(user),
                IsAdmin = usersInAdminRole.Contains(user),
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
        return await _userManager.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == token);
    }
}
