// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Derives the own-admin setup gate from the seeded admin account on every call (nothing is persisted): active while the
/// seeded admin is not deactivated, outside Development and the Playground. Also answers whether the first-start path
/// may claim the installation without signing in.
/// </summary>
/// <param name="userManagementService">Reads the seeded admin, its roles and the number of active administrators</param>
/// <param name="configuration">Provides the Playground switch (Deployment:IsPlayground)</param>
/// <param name="environment">Development never enforces the gate</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Microsoft.Extensions.Hosting;

namespace Klacks.Api.Application.Services.Authentication;

public class AdminSetupGateService : IAdminSetupGateService
{
    private const int SeedAdminOnlyAdministratorCount = 1;

    private readonly IUserManagementService _userManagementService;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public AdminSetupGateService(
        IUserManagementService userManagementService,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _userManagementService = userManagementService;
        _configuration = configuration;
        _environment = environment;
    }

    public async Task<bool> IsGateActiveAsync()
    {
        var isExempt = IsExempt();
        if (isExempt)
        {
            return false;
        }

        var seedAdmin = await _userManagementService.FindUserByIdAsync(SystemAccounts.SeedAdminUserId);
        var seedAdminStillActive = seedAdmin != null && seedAdmin.DeactivatedAt is null;

        return RequireOwnAdminGateDecision.Decide(isExempt, seedAdminStillActive);
    }

    public async Task<bool> IsFirstStartSetupOpenAsync()
    {
        if (IsExempt())
        {
            return false;
        }

        var seedAdmin = await _userManagementService.FindUserByIdNoTrackingAsync(SystemAccounts.SeedAdminUserId);
        if (seedAdmin == null
            || seedAdmin.DeactivatedAt is not null
            || !string.Equals(seedAdmin.PasswordHash, SystemAccounts.SeedAdminInitialPasswordHash, StringComparison.Ordinal))
        {
            return false;
        }

        var seedAdminRoles = await _userManagementService.GetUserRolesAsync(seedAdmin);
        if (!seedAdminRoles.Contains(Roles.Admin, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return await _userManagementService.CountActiveAdministratorsAsync() == SeedAdminOnlyAdministratorCount;
    }

    public Task<bool> IsExemptAsync()
    {
        return Task.FromResult(IsExempt());
    }

    private bool IsExempt()
    {
        var isPlayground = _configuration.GetValue<bool>(DeploymentConstants.IsPlaygroundConfigKey, false);
        return isPlayground || _environment.IsDevelopment();
    }
}
