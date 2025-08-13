using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Repositories;

public interface IAbsenceDomainRepository
{
    Task<Absence?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<AbsenceSummary>> SearchAsync(AbsenceSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    Task<List<AbsenceSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<Absence> AddAsync(Absence absence, CancellationToken cancellationToken = default);
    
    Task<Absence> UpdateAsync(Absence absence, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    
    // Domain-specific methods
    Task<List<AbsenceSummary>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    
    Task<List<AbsenceSummary>> GetByGroupIdAsync(int groupId, bool includeSubGroups = false, CancellationToken cancellationToken = default);
    
    Task<List<AbsenceSummary>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    
    Task<List<AbsenceSummary>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    
    Task<List<AbsenceSummary>> GetApprovedAbsencesAsync(CancellationToken cancellationToken = default);
    
    Task<bool> HasOverlappingAbsenceAsync(int clientId, DateOnly startDate, DateOnly endDate, int? excludeAbsenceId = null, CancellationToken cancellationToken = default);
    
    Task<int> GetAbsenceDaysCountAsync(int clientId, int year, CancellationToken cancellationToken = default);
    
    Task<List<AbsenceSummary>> GetByReasonAsync(string reason, CancellationToken cancellationToken = default);
}