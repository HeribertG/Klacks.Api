// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Staffs;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientAvailabilityRepository : IBaseRepository<ClientAvailability>
{
    Task<List<ClientAvailability>> GetByDateRange(DateOnly start, DateOnly end);

    Task<List<ClientAvailability>> GetByClientAndDateRange(Guid clientId, DateOnly start, DateOnly end);

    Task BulkUpsert(List<ClientAvailability> items);

    Task<List<ClientAvailabilityTotalResource>> GetTotalsByClientsAndDateRange(List<Guid> clientIds, DateOnly start, DateOnly end);
}
