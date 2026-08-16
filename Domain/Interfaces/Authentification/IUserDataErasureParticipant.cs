// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lets a component outside the core DbContext take part in a GDPR account erasure. Introduced
/// because plugin-owned tables carry personal data (messenger identities above all) but are not
/// DbSets on DataBaseContext, so UserDataEraser's curated list could never reach them.
/// The core registers no implementation of its own and resolves the participants as a collection:
/// with the plugin gone, the collection is empty and the erasure simply runs the core tables. It
/// follows the same shape as IOfflineMessengerNotifier - a core-owned interface with an adapter in
/// Infrastructure/Plugins - so no second bridging pattern enters the codebase.
/// Implementations must NOT skip work because a plugin is switched off: a disabled plugin leaves
/// its rows in the database, and skipping them is precisely the data-protection gap this closes.
/// </summary>
/// <param name="userId">AppUser identifier whose rows must disappear, in the string form Identity uses</param>

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IUserDataErasureParticipant
{
    /// <summary>
    /// Human-readable name of the data set this participant erases, used for the erasure log.
    /// </summary>
    string DataSetName { get; }

    Task<int> EraseAsync(string userId, CancellationToken cancellationToken = default);
}
