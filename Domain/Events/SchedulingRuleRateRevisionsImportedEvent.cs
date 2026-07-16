// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

/// <summary>
/// Raised after a region-setup import committed actual inserts or updates to scheduling rule rate
/// revisions or to surcharge-relevant scheduling rule preset fields. Never raised for a no-op import.
/// </summary>
/// <param name="RuleIds">The scheduling rules whose revisions or surcharge fields actually changed</param>
/// <param name="EarliestChangedValidFrom">Earliest ValidFrom among the changed rate revisions; null when only rule preset surcharge fields changed</param>
public sealed record SchedulingRuleRateRevisionsImportedEvent(
    IReadOnlyCollection<Guid> RuleIds,
    DateOnly? EarliestChangedValidFrom) : DomainEvent;
