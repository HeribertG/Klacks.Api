// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Persistence of the per-account intro-tour state. Stage-only except <see cref="EnsureExistsAsync"/>: changes to a
/// tracked row are committed by the caller via IUnitOfWork.CompleteAsync. EnsureExistsAsync runs one atomic
/// INSERT ... ON CONFLICT DO NOTHING statement that is committed on its own and flushes nothing else.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IUserIntroTourStateRepository
{
    /// <summary>
    /// The user's state without change tracking, or null when the account has no row yet.
    /// </summary>
    Task<UserIntroTourState?> GetNoTrackingAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// The user's tracked state for an update, or null when the account has no row yet.
    /// </summary>
    Task<UserIntroTourState?> GetForUpdateAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a row with the given status when the account has none; an existing row stays untouched, so two
    /// concurrent first writes cannot collide on the primary key.
    /// </summary>
    /// <param name="userId">AspNetUsers id of the account</param>
    /// <param name="initialStatus">Status of a newly inserted row</param>
    /// <param name="nowUtc">Creation timestamp of a newly inserted row</param>
    Task EnsureExistsAsync(string userId, string initialStatus, DateTime nowUtc, CancellationToken cancellationToken = default);
}
