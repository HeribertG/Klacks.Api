// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the caller's Admin and Authorised roles from the HTTP context and applies the Work lock rule, so the
/// Work, expense and WorkChange handlers share one role lookup instead of each reading the claims themselves.
/// </summary>
/// <param name="guard">The domain guard deciding the lock rule</param>
/// <param name="parentWork">The Work a child entry belongs to (child variant)</param>
/// <param name="work">The stored Work about to be updated or deleted (Work variant)</param>
/// <param name="childWorks">The sub-works of a container about to be saved, moved or deleted (children variant)</param>
/// <param name="childBreaks">The sub-breaks of that container (children variant)</param>
/// <param name="children">The live sub-works and sub-breaks of the containers about to be moved or deleted; a delete refuses every sealed break for a non-admin</param>
/// <param name="httpContextAccessor">Source of the calling user's roles; no user means no role</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Schedules;

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

    public static void EnsureChildrenWritableForCaller(
        this IParentWorkLockGuard guard,
        IEnumerable<Work> childWorks,
        IEnumerable<Break> childBreaks,
        IHttpContextAccessor httpContextAccessor)
    {
        var (isAdmin, isAuthorised) = ResolveRoles(httpContextAccessor);
        foreach (var work in childWorks)
        {
            guard.EnsureWorkWritable(work, isAdmin, isAuthorised);
        }

        foreach (var entry in childBreaks)
        {
            BreakLockRule.EnsureWritable(entry, isAdmin);
        }
    }

    public static void EnsureChildrenWritableForCaller(
        this IParentWorkLockGuard guard,
        ContainerChildren children,
        IHttpContextAccessor httpContextAccessor)
        => guard.EnsureChildrenWritableForCaller(children.Works, children.Breaks, httpContextAccessor);

    public static void EnsureChildrenDeletableForCaller(
        this IParentWorkLockGuard guard,
        ContainerChildren children,
        IHttpContextAccessor httpContextAccessor)
    {
        var (isAdmin, isAuthorised) = ResolveRoles(httpContextAccessor);
        foreach (var work in children.Works)
        {
            guard.EnsureWorkWritable(work, isAdmin, isAuthorised);
        }

        foreach (var entry in children.Breaks)
        {
            BreakLockRule.EnsureDeletable(entry, isAdmin);
        }
    }

    private static (bool IsAdmin, bool IsAuthorised) ResolveRoles(IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        return (user?.IsInRole(Roles.Admin) == true, user?.IsInRole(Roles.Authorised) == true);
    }
}
