// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default IWorkRestoreAuthorizer reading the caller from the HTTP context: the Admin role grants Admin,
/// a NameIdentifier claim equal to the Work's CurrentUserDeleted (the value DataBaseContext stamps on
/// soft-delete) grants Owner, everything else - other user, no claim, no context, no stamp - is Hidden.
/// </summary>
/// <param name="httpContextAccessor">Caller identity: roles and NameIdentifier claim</param>

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using System.Security.Claims;

namespace Klacks.Api.Application.Services.Schedules;

public class WorkRestoreAuthorizer : IWorkRestoreAuthorizer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WorkRestoreAuthorizer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public WorkRestoreAccess Resolve(Work work)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return WorkRestoreAccess.Hidden;
        }

        if (user.IsInRole(Roles.Admin))
        {
            return WorkRestoreAccess.Admin;
        }

        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(work.CurrentUserDeleted))
        {
            return WorkRestoreAccess.Hidden;
        }

        return string.Equals(work.CurrentUserDeleted, userId, StringComparison.Ordinal)
            ? WorkRestoreAccess.Owner
            : WorkRestoreAccess.Hidden;
    }
}
