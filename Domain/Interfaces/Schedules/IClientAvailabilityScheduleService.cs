// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IClientAvailabilityScheduleService
{
    /// <summary>
    /// Reads the availability ranges per client and day from get_client_availability_for_schedule, materialised, in
    /// the function's order.
    /// </summary>
    /// <param name="startDate">First day, inclusive</param>
    /// <param name="endDate">Last day, inclusive</param>
    /// <param name="clientIds">Clients to read; empty = no rows</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<IReadOnlyList<ClientAvailabilityScheduleEntry>> GetClientAvailabilityAsync(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default);
}
