// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.ClientAvailabilitySchedule;

/// <summary>
/// Reads the per-client, per-day availability ranges from get_client_availability_for_schedule.
/// </summary>
/// <param name="context">Database context the function is called on</param>
public class ClientAvailabilityScheduleService : IClientAvailabilityScheduleService
{
    private readonly DataBaseContext _context;

    public ClientAvailabilityScheduleService(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClientAvailabilityScheduleEntry>> GetClientAvailabilityAsync(
        DateOnly startDate,
        DateOnly endDate,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default)
    {
        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var clientIdArray = clientIds.ToArray();

        return await _context.ClientAvailabilityScheduleEntries
            .FromSqlInterpolated($@"
                SELECT * FROM get_client_availability_for_schedule(
                    {startDateTime}::DATE,
                    {endDateTime}::DATE,
                    {clientIdArray}::UUID[]
                )")
            .ToListAsync(cancellationToken);
    }
}
