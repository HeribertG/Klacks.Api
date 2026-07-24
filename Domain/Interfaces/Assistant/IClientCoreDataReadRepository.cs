// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Read-only repository for core-data quality scans. Used by ClientMissingCoreDataDetector to
/// list clients with a membership valid on the reference date that lack an active address or
/// any e-mail/phone communication entry.
/// </summary>

using Klacks.Api.Domain.DTOs.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IClientCoreDataReadRepository
{
    Task<List<ClientCoreDataStatus>> GetActiveClientsWithMissingCoreDataAsync(
        DateOnly referenceDate,
        int maxResults,
        CancellationToken cancellationToken = default);
}
