// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Write path for K12 stage-1 compensatory-rest tracking. For one client and a check window it detects
/// rest-shortfall gaps (using the same schedule-block source as the live validator), upserts an
/// obligation per gap keyed by (ClientId, RestGapStart), marks fulfilment when a later extended rest
/// compensates the shortfall before the deadline, and soft-deletes obligations whose triggering gap was
/// repaired. Idempotent: reconciling the same window twice yields the same rows. Real mode only —
/// a non-null analyseToken is a no-op so scenario placements never create real obligations.
/// </summary>

namespace Klacks.Api.Application.Interfaces.Schedules;

public interface ICompensatoryRestObligationReconciler
{
    Task ReconcileAsync(
        Guid clientId,
        DateOnly windowStart,
        DateOnly windowEnd,
        Guid? analyseToken = null,
        CancellationToken cancellationToken = default);
}
