// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface IClientAvailabilityRepository : IBaseRepository<ClientAvailability>
{
    Task<List<ClientAvailability>> GetByDateRange(DateOnly start, DateOnly end);

    Task<List<ClientAvailability>> GetByClientAndDateRange(Guid clientId, DateOnly start, DateOnly end);

    Task BulkUpsert(List<ClientAvailability> items);
}
