// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.ClientAvailabilitySchedule;

public class ClientAvailabilityScheduleService : IClientAvailabilityScheduleService
{
    private readonly DataBaseContext _context;

    public ClientAvailabilityScheduleService(DataBaseContext context)
    {
        _context = context;
    }

    public IQueryable<ClientAvailabilityScheduleEntry> GetClientAvailabilityQuery(
        DateOnly startDate,
        DateOnly endDate,
        List<Guid> clientIds)
    {
        var startDateTime = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var endDateTime = DateTime.SpecifyKind(endDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        var clientIdArray = clientIds.ToArray();

        return _context.ClientAvailabilityScheduleEntries
            .FromSqlInterpolated($@"
                SELECT * FROM get_client_availability_for_schedule(
                    {startDateTime}::DATE,
                    {endDateTime}::DATE,
                    {clientIdArray}::UUID[]
                )");
    }
}
