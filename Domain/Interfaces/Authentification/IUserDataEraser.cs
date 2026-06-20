// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Permanently erases a user's personal assistant data (GDPR Article 17, right to erasure) when the
/// user account is deleted. Hard-deletes — bypassing the soft-delete interceptor — so the data is
/// physically removed, not merely flagged. Covers the user-scoped assistant rows that have no
/// foreign-key cascade to the identity user and would otherwise be orphaned.
/// </summary>
public interface IUserDataEraser
{
    Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default);
}
