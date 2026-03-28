// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IClientShiftPreferenceRepository : IBaseRepository<ClientShiftPreference>
{
    Task<List<ClientShiftPreference>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);

    Task DeleteAllByClientIdAsync(Guid clientId, CancellationToken ct = default);
}
