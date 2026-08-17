// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

public interface IUserAbsencePeriodRepository
{
    Task<IReadOnlyList<UserAbsencePeriod>> GetByUserIdAsync(string appUserId, CancellationToken cancellationToken = default);

    Task<UserAbsencePeriod> AddAsync(UserAbsencePeriod period, CancellationToken cancellationToken = default);

    /// <summary>Returns false when no matching row exists (stale client-side id).</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
