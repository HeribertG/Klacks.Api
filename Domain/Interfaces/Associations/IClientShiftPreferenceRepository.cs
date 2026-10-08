// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Associations;

public interface IClientShiftPreferenceRepository : IBaseRepository<ClientShiftPreference>
{
    Task<List<ClientShiftPreference>> GetByClientIdAsync(Guid clientId, CancellationToken ct = default);

    Task<List<ClientShiftPreference>> GetByShiftIdAsync(Guid shiftId, CancellationToken ct = default);

    Task<ClientShiftPreference?> GetByClientAndShiftAsync(Guid clientId, Guid shiftId, CancellationToken ct = default);

    Task DeleteAllByClientIdAsync(Guid clientId, CancellationToken ct = default);

    /// <summary>
    /// The preferences of the clients for one world (analyseToken null = real plan), each also handed down the order
    /// tree to the cut pieces it reaches (<see cref="Klacks.Api.Domain.Services.Schedules.ShiftScopeExpander"/>), the
    /// same source Wizard 1 and the harmonizer read.
    /// </summary>
    Task<IReadOnlyList<ScopedShiftPreference>> GetScopedByClientIdsAsync(
        IReadOnlyCollection<Guid> clientIds, Guid? analyseToken, CancellationToken ct = default);
}
