using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces;

/// <summary>
/// Domain service for refresh token management
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Creates a new refresh token for the user
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>New refresh token</returns>
    Task<string> CreateRefreshTokenAsync(string userId);

    /// <summary>
    /// Validates a refresh token for a user
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <param name="refreshToken">Token to validate</param>
    /// <returns>True if token is valid</returns>
    Task<bool> ValidateRefreshTokenAsync(string userId, string refreshToken);

    /// <summary>
    /// Gets user from refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>User if token is valid</returns>
    Task<AppUser?> GetUserFromRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Removes all refresh tokens for a user
    /// </summary>
    /// <param name="userId">User's ID</param>
    Task RemoveAllUserRefreshTokensAsync(string userId);

    /// <summary>
    /// Calculates expiry time for access tokens
    /// </summary>
    /// <returns>Token expiry time</returns>
    DateTime CalculateTokenExpiryTime();

    /// <summary>
    /// Calculates expiry time for refresh tokens
    /// </summary>
    /// <returns>Refresh token expiry time</returns>
    DateTime CalculateRefreshTokenExpiryTime();
}