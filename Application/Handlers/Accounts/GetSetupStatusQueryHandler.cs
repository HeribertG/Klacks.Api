// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Answers GET Setup/Status: whether the seeded admin still has to be replaced, plus the password rules the
/// registration enforces, so the setup form can show them before the first submit. The rules are read from the
/// effective IdentityOptions, never from a second copy, so the form cannot drift from what Identity checks.
/// The anonymous first-start probe answers with the first-start eligibility instead of the gate and leaves the
/// password rules out once the installation can no longer be claimed, so it reveals nothing beyond one boolean.
/// </summary>
/// <param name="adminSetupGateService">Tells whether the own-admin setup is still required</param>
/// <param name="identityOptions">The configured Identity options whose password section is published</param>

using Klacks.Api.Application.DTOs.Setup;
using Klacks.Api.Application.Queries.Accounts;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Application.Handlers.Accounts;

public class GetSetupStatusQueryHandler : IRequestHandler<GetSetupStatusQuery, SetupStatusResource>
{
    private readonly IAdminSetupGateService _adminSetupGateService;
    private readonly IOptions<IdentityOptions> _identityOptions;

    public GetSetupStatusQueryHandler(
        IAdminSetupGateService adminSetupGateService,
        IOptions<IdentityOptions> identityOptions)
    {
        _adminSetupGateService = adminSetupGateService;
        _identityOptions = identityOptions;
    }

    public async Task<SetupStatusResource> Handle(GetSetupStatusQuery request, CancellationToken cancellationToken)
    {
        var requiresOwnAdmin = request.IsFirstStart
            ? await _adminSetupGateService.IsFirstStartSetupOpenAsync()
            : await _adminSetupGateService.IsGateActiveAsync();

        return new SetupStatusResource
        {
            RequiresOwnAdmin = requiresOwnAdmin,
            PasswordPolicy = request.IsFirstStart && !requiresOwnAdmin ? null : BuildPasswordPolicy(),
        };
    }

    private PasswordPolicyResource BuildPasswordPolicy()
    {
        var password = _identityOptions.Value.Password;

        return new PasswordPolicyResource
        {
            RequiredLength = password.RequiredLength,
            RequireDigit = password.RequireDigit,
            RequireNonAlphanumeric = password.RequireNonAlphanumeric,
            RequireUppercase = password.RequireUppercase,
            RequireLowercase = password.RequireLowercase,
        };
    }
}
