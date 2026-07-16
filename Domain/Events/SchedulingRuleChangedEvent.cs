// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

/// <summary>
/// Raised after a committed create, update or delete of a scheduling rule whose surcharge-relevant
/// values (rates, night window, overtime threshold) may affect persisted work surcharges.
/// </summary>
/// <param name="RuleId">The affected scheduling rule</param>
public sealed record SchedulingRuleChangedEvent(Guid RuleId) : DomainEvent;
