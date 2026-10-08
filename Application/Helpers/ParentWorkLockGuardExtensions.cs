// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the caller's Admin and Authorised roles from the HTTP context and applies the Work lock rule, so the
/// Work, expense and WorkChange handlers share one role lookup instead of each reading the claims themselves.
/// </summary>
/// <param name="guard">The domain guard deciding the lock rule</param>
/// <param name="parentWork">The Work a child entry belongs to (child variant)</param>
/// <param name="work">The stored Work about to be updated or deleted (Work variant)</param>
/// <param name="httpContextAccessor">Source of the calling user's roles; no user means no role</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Helpers;

public static class ParentWorkLockGuardExtensions
{
    public static void EnsureChildWritableForCaller(
        this IParentWorkLockGuard guard,
        Work parentWork,
        IHttpContextAccessor httpContextAccessor)
    {
        var (isAdmin, isAuthorised) = ResolveRoles(httpContextAccessor);
        guard.EnsureChildWritable(parentWork, isAdmin, isAuthorised);
    }

    public static void EnsureWorkWritableForCaller(
        this IParentWorkLockGuard guard,
        Work work,
        IHttpContextAccessor httpContextAccessor)
    {
        var (isAdmin, isAuthorised) = ResolveRoles(httpContextAccessor);
        guard.EnsureWorkWritable(work, isAdmin, isAuthorised);
    }

    private static (bool IsAdmin, bool IsAuthorised) ResolveRoles(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return (user?.IsInRole(Roles.Admin) == true, user?.IsInRole(Roles.Authorised) == true);
    }
}
