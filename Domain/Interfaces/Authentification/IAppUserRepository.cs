// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Persistence of AppUser fields that are not owned by ASP.NET Identity's UserManager.
/// Self-committing: every method calls SaveChangesAsync itself, so it must not be mixed with staged,
/// uncommitted writes of a stage-only repository in the same scope.
/// </summary>
public interface IAppUserRepository
{
    /// <summary>
    /// Sets AppUser.DisplayOrder to the 1-based position of each id in the list and commits. Ids without a
    /// user are skipped but still consume their position; users missing from the list keep their order.
    /// </summary>
    /// <param name="orderedUserIds">User ids in the submitted display order</param>
    /// <param name="cancellationToken">Cancels the read and the commit</param>
    Task ApplyDisplayOrderAsync(IReadOnlyList<string> orderedUserIds, CancellationToken cancellationToken = default);
}
