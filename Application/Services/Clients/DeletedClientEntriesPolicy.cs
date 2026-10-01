// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Security.Claims;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Services.Clients;

/// <summary>
/// Soft-deleted clients are an administrator-only view: every client list or export that honours the
/// ShowDeleteEntries flag clears it for any caller who is not an administrator, including background
/// callers without a user.
/// </summary>
public static class DeletedClientEntriesPolicy
{
    /// <summary>
    /// Clears ShowDeleteEntries unless the caller is an administrator.
    /// </summary>
    /// <param name="clientFilter">Filter whose deleted-entries flag is checked and possibly cleared</param>
    /// <param name="user">Calling user, or null when no HTTP user is present</param>
    /// <returns>True when the flag was requested and has been cleared</returns>
    public static bool RestrictToAdmins(ClientFilter clientFilter, ClaimsPrincipal? user)
    {
        if (!clientFilter.ShowDeleteEntries || user?.IsInRole(Roles.Admin) == true)
        {
            return false;
        }

        clientFilter.ShowDeleteEntries = false;
        return true;
    }
}
