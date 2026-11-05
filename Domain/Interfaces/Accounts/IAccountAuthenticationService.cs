// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs.Registrations;

namespace Klacks.Api.Domain.Interfaces.Accounts;

public interface IAccountAuthenticationService
{
    Task<AuthenticatedResult> LogInUserAsync(string email, string password);

    Task<AuthenticatedResult> RefreshTokenAsync(RefreshRequestResource model);

    Task<AuthenticatedResult> MigrateRefreshTokenAsync(RefreshRequestResource model);

    Task<AuthenticatedResult> GenerateAuthenticationAsync(AppUser user, bool withRefreshToken = true, string? existingRefreshToken = null);

    Task<AuthenticatedResult> SetAuthenticatedResultAsync(AuthenticatedResult authenticatedResult, AppUser user, DateTime expires);

    void SetModelError(AuthenticatedResult model, string key, string message);
}