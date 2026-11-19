// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Materialising AppUser reads over ASP.NET Identity's own user set (UserManager.Users) in the request's context, so a
/// tracked user can be changed and saved through UserManager afterwards. Tracking is stated per read.
/// </summary>
public interface IAppUserReader
{
    /// <summary>
    /// Every account that has not been deactivated, in DisplayOrder, tracked like UserManager.Users.
    /// </summary>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<List<AppUser>> GetActiveUsersInDisplayOrderAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// The account whose stored password-reset token equals the given hash, tracked (the reset flow changes it and saves
    /// it through UserManager); null when none matches. Neither expiry nor deactivation is checked here.
    /// </summary>
    /// <param name="tokenHash">Hash of the reset token as PasswordResetTokenHasher produces it</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<AppUser?> FindByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// The account with this id, untracked, so a just-written user is re-read as a fresh database row; null when none.
    /// </summary>
    /// <param name="userId">Identity user id</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<AppUser?> FindByIdNoTrackingAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The account linked to this directory entry of this provider, tracked (sign-in updates its lockout counter through
    /// UserManager); null when none. Deactivated accounts are returned as well.
    /// </summary>
    /// <param name="identityProviderId">Directory provider id</param>
    /// <param name="directoryExternalId">LdapUserEntry.StableId of the entry</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<AppUser?> FindByDirectoryLinkAsync(Guid identityProviderId, string directoryExternalId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Every account linked to this provider, deactivated ones included, untracked.
    /// </summary>
    /// <param name="identityProviderId">Directory provider id</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<List<AppUser>> GetByIdentityProviderAsync(Guid identityProviderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The given e-mail addresses that an account already uses (compared as normalized by Identity), in their input
    /// spelling, in one query. Deactivated accounts count as well.
    /// </summary>
    /// <param name="emails">E-mail addresses as read from the directory</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<HashSet<string>> GetEmailsInUseAsync(IReadOnlyCollection<string> emails, CancellationToken cancellationToken = default);

    /// <summary>
    /// The ids of the given accounts that hold at least one role, in one query.
    /// </summary>
    /// <param name="userIds">Account ids</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<HashSet<string>> GetUserIdsWithRolesAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// The account linked to this directory external id on a soft-deleted identity provider, tracked; null when none
    /// or more than one.
    /// </summary>
    /// <param name="directoryExternalId">Normalized GUID external id (objectGUID or entryUUID)</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<AppUser?> FindLinkedToDeletedProviderAsync(string directoryExternalId, CancellationToken cancellationToken = default);
}
