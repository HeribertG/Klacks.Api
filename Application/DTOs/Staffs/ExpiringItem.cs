// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A single expiring assignment surfaced by list_expiring_contracts: either a client contract
/// assignment or a client qualification, discriminated by Kind.
/// </summary>
/// <param name="ClientId">Id of the client (employee or extern employee) holding the item.</param>
/// <param name="ClientName">Display name of the client.</param>
/// <param name="Kind">Either "Contract" or "Qualification".</param>
/// <param name="Name">Name of the contract or qualification that is expiring.</param>
/// <param name="ValidUntil">Date the assignment expires.</param>
/// <param name="DaysUntilExpiry">Number of days from the reference date until ValidUntil.</param>

namespace Klacks.Api.Application.DTOs.Staffs;

public record ExpiringItem(
    Guid ClientId,
    string ClientName,
    string Kind,
    string Name,
    DateOnly ValidUntil,
    int DaysUntilExpiry);
