// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

/// <summary>
/// Raised after a committed change to a contract or to a client-contract assignment whose values
/// influence persisted work surcharges (rates, night window, validity dates, scheduling rule binding).
/// </summary>
/// <param name="ContractId">The changed contract</param>
/// <param name="ClientId">The affected client when the change came from a client-contract assignment; null when the contract itself changed (all assigned clients are affected)</param>
/// <param name="RecalculationFrom">Start of the affected window: the earlier of the old and new validity start</param>
/// <param name="RecalculationUntil">End of the affected window; null lets the handler derive it from the latest existing work of the affected clients</param>
public sealed record ContractChangedEvent(
    Guid ContractId,
    Guid? ClientId,
    DateOnly RecalculationFrom,
    DateOnly? RecalculationUntil) : DomainEvent;
