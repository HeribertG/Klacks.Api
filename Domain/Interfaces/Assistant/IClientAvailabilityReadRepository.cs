// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only repository for availability-gap scans. Used by AvailabilityGapDetector to find
/// plannable clients (valid membership in the window) without a single availability entry
/// inside the window.
/// </summary>

using Klacks.Api.Domain.DTOs.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IClientAvailabilityReadRepository
{
    Task<bool> AnyAvailabilityEntriesExistAsync(CancellationToken cancellationToken = default);

    Task<List<PlannableClientInfo>> GetPlannableClientsWithoutAvailabilityAsync(
        DateOnly fromInclusive,
        DateOnly untilInclusive,
        int maxResults,
        CancellationToken cancellationToken = default);
}
