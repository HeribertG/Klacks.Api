// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IClientShiftPreferenceRepository : IBaseRepository<ClientShiftPreference>
{
    Task<List<ClientShiftPreference>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);

    Task<List<ClientShiftPreference>> GetByShiftIdAsync(Guid shiftId, CancellationToken ct = default);

    Task<ClientShiftPreference?> GetByClientAndShiftAsync(Guid clientId, Guid shiftId, CancellationToken ct = default);

    Task DeleteAllByClientIdAsync(Guid clientId, CancellationToken ct = default);
}
