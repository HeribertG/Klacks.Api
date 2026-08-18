// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Domain.DTOs;
using Klacks.Api.Domain.DTOs.Registrations;

namespace Klacks.Api.Domain.Interfaces.Accounts;

public interface IAccountManagementService
{
    Task<HttpResultResource> ChangeRoleUserAsync(ChangeRole editUserRole);

    Task<HttpResultResource> DeactivateAccountUserAsync(Guid id, string deactivatedBy);

    Task<HttpResultResource> ReactivateAccountUserAsync(Guid id);

    Task<List<UserResource>> GetUserListAsync();

    Task<HttpResultResource> UpdateAccountAsync(UpdateAccountResource updateAccount);

    /// <summary>Sets AppUser.DisplayOrder to the 1-based position of each id in orderedUserIds. A user
    /// id not found is skipped (stale client-side list).</summary>
    Task<HttpResultResource> ReorderUsersAsync(IReadOnlyList<string> orderedUserIds);
}