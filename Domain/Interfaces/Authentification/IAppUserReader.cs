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
}
