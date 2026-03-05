// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IClientContractDataProvider
{
    Task<EffectiveContractData> GetEffectiveContractDataAsync(Guid clientId, DateOnly date);
    Task<Dictionary<Guid, EffectiveContractData>> GetEffectiveContractDataForClientsAsync(List<Guid> clientIds, DateOnly date);
}
