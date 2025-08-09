using Klacks.Api.Models.Authentification;
using Klacks.Api.Presentation.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Klacks.Api.Interfaces.Domains;

/// <summary>
/// Domain service for handling authentication operations
/// </summary>
public interface IAuthenticationService
{
    /// <summary>
    /// Validates user credentials (email and password)
    /// </summary>
    /// <param name="email">User's email</param>
    /// <param name="password">User's password</param>
    /// <returns>Validation result and user if valid</returns>
    Task<(bool IsValid, AppUser? User)> ValidateCredentialsAsync(string email, string password);

    /// <summary>
    /// Changes a user's password
    /// </summary>
    /// <param name="user">The user</param>
    /// <param name="oldPassword">Current password</param>
    /// <param name="newPassword">New password</param>
    /// <returns>Success status and identity result</returns>
    Task<(bool Success, IdentityResult? Result)> ChangePasswordAsync(AppUser user, string oldPassword, string newPassword);

    /// <summary>
    /// Extracts user from JWT access token
    /// </summary>
    /// <param name="token">JWT access token</param>
    /// <returns>User if token is valid</returns>
    Task<AppUser?> GetUserFromAccessTokenAsync(string token);

    /// <summary>
    /// Adds Identity errors to ModelState
    /// </summary>
    /// <param name="identityResult">Identity result with errors</param>
    /// <param name="modelState">ModelState to add errors to</param>
    /// <returns>Updated ModelState</returns>
    ModelStateDictionary AddErrorsToModelState(IdentityResult identityResult, ModelStateDictionary? modelState = null);

    /// <summary>
    /// Sets a model error in AuthenticatedResult
    /// </summary>
    /// <param name="model">The authenticated result</param>
    /// <param name="key">Error key</param>
    /// <param name="message">Error message</param>
    void SetModelError(AuthenticatedResult model, string key, string message);
}