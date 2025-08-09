using Klacks.Api.Models.Authentification;
using Klacks.Api.Presentation.Resources;
using Klacks.Api.Presentation.Resources.Registrations;
using Microsoft.AspNetCore.Identity;

namespace Klacks.Api.Interfaces.Domains;

/// <summary>
/// Domain service for user management operations
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="user">User to register</param>
    /// <param name="password">User's password</param>
    /// <returns>Success status and identity result</returns>
    Task<(bool Success, IdentityResult? Result)> RegisterUserAsync(AppUser user, string password);

    /// <summary>
    /// Finds a user by email address
    /// </summary>
    /// <param name="email">User's email</param>
    /// <returns>User if found</returns>
    Task<AppUser?> FindUserByEmailAsync(string email);

    /// <summary>
    /// Finds a user by ID
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>User if found</returns>
    Task<AppUser?> FindUserByIdAsync(string userId);

    /// <summary>
    /// Changes a user's role membership
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <param name="roleName">Role name</param>
    /// <param name="isSelected">Whether to add or remove the role</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> ChangeUserRoleAsync(string userId, string roleName, bool isSelected);

    /// <summary>
    /// Deletes a user account
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>Success status and message</returns>
    Task<(bool Success, string Message)> DeleteUserAsync(Guid userId);

    /// <summary>
    /// Gets list of all users with their roles
    /// </summary>
    /// <returns>List of user resources</returns>
    Task<List<UserResource>> GetUserListAsync();

    /// <summary>
    /// Checks if user is in specific role
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="role">Role name</param>
    /// <returns>True if user is in role</returns>
    Task<bool> IsUserInRoleAsync(AppUser user, string role);
}