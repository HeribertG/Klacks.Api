// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A detector that can name the COMPLETE set of fingerprints its kind currently holds, independent of
/// the cap its DetectAsync applies. Deliberately a separate interface rather than a member of
/// IAgentTriggerDetector: reporting a capped page as complete would make the ledger resolve and re-arm
/// everything beyond the cap on every tick (see IAgentConditionLedgerService.MarkResolvedAsync), so
/// implementing this is an explicit promise a detector makes, not a default all of them inherit. The
/// tick reconciles resolutions only for detectors that made the promise; the rest keep their open rows.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IAgentConditionFingerprintSource
{
    /// <summary>
    /// Every fingerprint the kind currently holds: the same business predicates DetectAsync applies,
    /// without its cap, without hydrating entities. Must be a superset of the fingerprints carried by
    /// the events the same tick's DetectAsync returned - a narrower set would resolve rows that the
    /// very same tick opened, and re-arm them as new ones on the next.
    /// </summary>
    Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default);
}
