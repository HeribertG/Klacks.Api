// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Scheduling;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Query surface for determining which clients and which date window are affected by a
/// retroactive contract or scheduling-rule change before queueing a thorough recalculation.
/// </summary>
public interface ISurchargeRecalculationScope
{
    Task<List<Guid>> GetClientIdsForContractAsync(Guid contractId, CancellationToken cancellationToken = default);

    Task<List<ContractRecalculationWindow>> GetContractWindowsForRulesAsync(IReadOnlyCollection<Guid> ruleIds, CancellationToken cancellationToken = default);

    Task<List<Guid>> GetClientIdsForContractsAsync(IReadOnlyCollection<Guid> contractIds, CancellationToken cancellationToken = default);

    Task<DateOnly?> GetLatestWorkDateAsync(IReadOnlyCollection<Guid> clientIds, DateOnly from, CancellationToken cancellationToken = default);

    Task<bool> HasWorksInWindowAsync(IReadOnlyCollection<Guid> clientIds, DateOnly from, DateOnly until, CancellationToken cancellationToken = default);

    Task<WorkRecalculationWindow?> GetUnlockedRealWorkWindowAsync(CancellationToken cancellationToken = default);
}
