// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves the effective contract data of several clients, each on its own day, with one provider call per distinct
/// day instead of one per client.
/// </summary>
/// <param name="provider">Resolves the effective contract data of a list of clients on one day</param>
/// <param name="clientDates">The clients and the day each one is resolved on</param>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public static class ClientContractDataBatch
{
    public static async Task<Dictionary<(Guid ClientId, DateOnly Date), EffectiveContractData>> LoadAsync(
        IClientContractDataProvider provider,
        IEnumerable<(Guid ClientId, DateOnly Date)> clientDates)
    {
        var result = new Dictionary<(Guid ClientId, DateOnly Date), EffectiveContractData>();

        foreach (var day in clientDates.Distinct().GroupBy(c => c.Date, c => c.ClientId))
        {
            var perClient = await provider.GetEffectiveContractDataForClientsAsync(day.ToList(), day.Key);
            foreach (var (clientId, data) in perClient)
            {
                result[(clientId, day.Key)] = data;
            }
        }

        return result;
    }
}
