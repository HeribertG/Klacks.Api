// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleCommandRepository : IBaseRepository<ScheduleCommand>
{
    /// <summary>
    /// Returns the schedule commands for the given clients within the inclusive date window and scenario
    /// token. A null <paramref name="analyseToken"/> selects the real plan (commands not tied to a scenario).
    /// </summary>
    /// <param name="clientIds">The clients whose commands are loaded</param>
    /// <param name="from">Inclusive start of the date window</param>
    /// <param name="until">Inclusive end of the date window</param>
    /// <param name="analyseToken">Scenario token; null selects the real (non-scenario) plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<ScheduleCommand>> GetByClientsAndDateRangeAsync(
        IReadOnlyList<Guid> clientIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken cancellationToken);
}
