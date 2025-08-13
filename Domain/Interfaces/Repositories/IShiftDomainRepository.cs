using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Repositories;

public interface IShiftDomainRepository
{
    Task<Shift?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<ShiftSummary>> SearchAsync(ShiftSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<Shift> AddAsync(Shift shift, CancellationToken cancellationToken = default);
    
    Task<Shift> UpdateAsync(Shift shift, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    
    // Domain-specific methods
    Task<List<ShiftSummary>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetByGroupIdAsync(int groupId, bool includeSubGroups = false, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetByStatusAsync(ShiftStatus status, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetByTypeAsync(ShiftType type, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetScheduledShiftsAsync(CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetCompletedShiftsAsync(CancellationToken cancellationToken = default);
    
    Task<bool> HasConflictingShiftAsync(int clientId, DateOnly date, TimeOnly startTime, TimeOnly endTime, int? excludeShiftId = null, CancellationToken cancellationToken = default);
    
    Task<TimeSpan> GetTotalWorkingHoursAsync(int clientId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetShiftsByDayOfWeekAsync(DayOfWeek dayOfWeek, CancellationToken cancellationToken = default);
    
    Task<List<ShiftSummary>> GetShiftsByLocationAsync(string location, CancellationToken cancellationToken = default);
}