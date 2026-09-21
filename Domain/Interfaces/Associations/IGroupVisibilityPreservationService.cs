// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Keeps the status quo when groups are introduced into an installation that had none. Without any
/// group every caller is unrestricted; the first group flips the group scope to fail-closed, so every
/// non-admin without a group_visibility row would abruptly see nothing. Introducing groups would
/// therefore be an implicit rights change. This service grants the users that were unrestricted until
/// then visibility on the newly created root group(s), so nothing changes for them until an
/// administrator deliberately restricts visibility. Fail-closed itself is not relaxed: users created
/// after the first group keep the fail-closed default. The state is per DI scope, which makes a bulk
/// creation decide once and still cover every root it creates.
/// </summary>
public interface IGroupVisibilityPreservationService
{
    /// <summary>
    /// True when the group about to be persisted is a root group (no parent) and the installation held
    /// no group at all when this scope started - the one moment at which visibility must be preserved.
    /// </summary>
    /// <param name="newGroup">The group that is about to be persisted, not yet saved</param>
    /// <param name="cancellationToken">Cancels the baseline query</param>
    Task<bool> RequiresPreservationAsync(Group newGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages one group_visibility row per affected user for the given root group and returns how many
    /// rows were staged. Idempotent: a user that already holds a row for this root is skipped. The rows
    /// are only staged, so the caller's SaveChanges commits them together with the group itself.
    /// </summary>
    /// <param name="newRoot">The newly created root group whose visibility is granted</param>
    /// <param name="cancellationToken">Cancels the queries for affected users and existing rows</param>
    Task<int> PreserveForNewRootAsync(Group newRoot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Number of users that would keep access to everything if the first group were created now, for
    /// the advisory a preview shows before the apply. Zero as soon as any group exists, because then
    /// the first-group event has already happened and nothing is preserved any more.
    /// </summary>
    /// <param name="cancellationToken">Cancels the baseline and user queries</param>
    Task<int> CountUsersKeepingFullVisibilityAsync(CancellationToken cancellationToken = default);
}
