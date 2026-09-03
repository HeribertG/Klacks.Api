// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Services.Accounts;
using Microsoft.Extensions.Hosting;

namespace Klacks.Api.Application.Services.Authentication;

public class AdminSetupGateService : IAdminSetupGateService
{
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
