// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ISupervisorOverrideAuthorizer"/>. Requires the caller to hold the Admin or
/// Authorised role and the COMPLIANCE_ENFORCEMENT_ALLOW_SUPERVISOR_OVERRIDE setting to allow it.
/// </summary>
/// <param name="enforcementResolver">Resolves whether a supervisor override is allowed at all (K1)</param>
/// <param name="httpContextAccessor">Resolves the caller's roles for the authorization check</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Microsoft.AspNetCore.Http;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class SupervisorOverrideAuthorizer : ISupervisorOverrideAuthorizer
{
    private readonly IComplianceEnforcementResolver _enforcementResolver;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SupervisorOverrideAuthorizer(
        IComplianceEnforcementResolver enforcementResolver,
        IHttpContextAccessor httpContextAccessor)
    {
        _enforcementResolver = enforcementResolver;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<bool> IsAuthorizedAsync(bool overrideBlockRequested)
    {
        if (!overrideBlockRequested)
        {
            return false;
        }

        var user = _httpContextAccessor.HttpContext?.User;
        var isSupervisor = user?.IsInRole(Roles.Admin) == true || user?.IsInRole(Roles.Authorised) == true;
        if (!isSupervisor)
        {
            return false;
        }

        return await _enforcementResolver.IsSupervisorOverrideAllowedAsync();
    }
}
