// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientAvailabilityRepository : IBaseRepository<ClientAvailability>
{
    Task<List<ClientAvailability>> GetByDateRange(DateOnly start, DateOnly end);

    Task<List<ClientAvailability>> GetByClientAndDateRange(Guid clientId, DateOnly start, DateOnly end);

    /// <summary>Stages insert or update per slot; a soft-deleted row of the same slot is revived (the slot key is unique).</summary>
    Task BulkUpsert(List<ClientAvailability> items);

    /// <summary>Stages a soft delete of the stored row of each slot; a slot without a row is skipped.</summary>
    Task BulkRemove(IReadOnlyCollection<ClientAvailabilitySlot> slots);

    Task<List<ClientAvailabilityTotalResource>> GetTotalsByClientsAndDateRange(List<Guid> clientIds, DateOnly start, DateOnly end);
}
