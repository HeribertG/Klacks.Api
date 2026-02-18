using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleChangeTracker
{
    Task TrackChangeAsync(Guid clientId, DateOnly changeDate);
    Task<List<ScheduleChangeResource>> GetChangesAsync(DateOnly startDate, DateOnly endDate);
}
