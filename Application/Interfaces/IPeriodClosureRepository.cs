using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IPeriodClosureRepository : IBaseRepository<PeriodClosure>
{
    Task<PeriodClosure?> GetOverlapping(DateOnly date);
    Task<List<PeriodClosure>> GetByDateRange(DateOnly startDate, DateOnly endDate);
    Task<bool> ExistsForDate(DateOnly date);
}
