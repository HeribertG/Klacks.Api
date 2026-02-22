// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Application.DTOs.Registrations;

namespace Klacks.Api.Domain.Interfaces.Accounts;

public interface IAccountAuthenticationService
{
    Task<AuthenticatedResult> LogInUserAsync(string email, string password);

    Task<AuthenticatedResult> RefreshTokenAsync(RefreshRequestResource model);

    Task<AuthenticatedResult> GenerateAuthenticationAsync(AppUser user, bool withRefreshToken = true);

    Task<AuthenticatedResult> SetAuthenticatedResultAsync(AuthenticatedResult authenticatedResult, AppUser user, DateTime expires);

    Task<bool> ValidateRefreshTokenAsync(AppUser user, string refreshToken);

    void SetModelErrorAsync(AuthenticatedResult model, string key, string message);
}