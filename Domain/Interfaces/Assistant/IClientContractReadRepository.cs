// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only repository for ClientContract scans. Used by ContractExpiringSoonDetector to
/// find contracts whose UntilDate falls inside a soft horizon without a follow-up contract
/// chained after them.
/// </summary>

using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IClientContractReadRepository
{
    Task<List<ClientContract>> GetExpiringBetweenAsync(DateOnly fromInclusive, DateOnly untilInclusive, CancellationToken cancellationToken = default);

    Task<List<ClientContract>> GetContractsForClientAsync(Guid clientId, CancellationToken cancellationToken = default);
}
