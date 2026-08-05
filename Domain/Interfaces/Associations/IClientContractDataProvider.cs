// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IClientContractDataProvider
{
    Task<EffectiveContractData> GetEffectiveContractDataAsync(Guid clientId, DateOnly date, int? paymentInterval = null);
    Task<Dictionary<Guid, EffectiveContractData>> GetEffectiveContractDataForClientsAsync(List<Guid> clientIds, DateOnly date, int? paymentInterval = null);

    /// <summary>
    /// Resolves the effective contract data for every day of a range in one pass. Callers that plan a
    /// whole period asked day by day, which repeated the same contract, revision and settings queries
    /// once per day. The result is identical to calling the per-day overload for each date.
    /// </summary>
    /// <param name="clientIds">Clients to resolve.</param>
    /// <param name="from">First day of the range, inclusive.</param>
    /// <param name="until">Last day of the range, inclusive.</param>
    /// <param name="paymentInterval">Optional payment-interval filter, as in the per-day overload.</param>
    Task<Dictionary<DateOnly, Dictionary<Guid, EffectiveContractData>>> GetEffectiveContractDataForClientsRangeAsync(
        List<Guid> clientIds, DateOnly from, DateOnly until, int? paymentInterval = null);
}
