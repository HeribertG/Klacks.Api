// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

/// <summary>
/// Raised after a committed change of the ACTIVE_INDUSTRIES setting. The setting itself only filters
/// selection lists, so nothing about existing contracts, rules or calculations changes with it. This
/// event is what makes the switch observable: contracts still referencing a rule of a now-inactive
/// industry are counted and their clients are re-validated, so the error list reflects the new state
/// without waiting for the next period change.
/// </summary>
/// <param name="PreviousValue">Comma-separated industry slugs that were active before the change</param>
/// <param name="CurrentValue">Comma-separated industry slugs that are active now</param>
public sealed record ActiveIndustriesChangedEvent(string? PreviousValue, string CurrentValue) : DomainEvent;
