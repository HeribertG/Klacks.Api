using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleChangeTracker
{
    Task TrackChangeAsync(Guid clientId, DateOnly changeDate);
    Task<List<ScheduleChange>> GetChangesAsync(DateOnly startDate, DateOnly endDate);
}
