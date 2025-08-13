using Klacks.Api.Domain.Models.Criteria;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Repositories;

public interface IBreakDomainRepository
{
    Task<Break?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    
    Task<PagedResult<BreakSummary>> SearchAsync(BreakSearchCriteria criteria, CancellationToken cancellationToken = default);
    
    Task<List<BreakSummary>> GetAllAsync(CancellationToken cancellationToken = default);
    
    Task<Break> AddAsync(Break @break, CancellationToken cancellationToken = default);
    
    Task<Break> UpdateAsync(Break @break, CancellationToken cancellationToken = default);
    
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    
    // Domain-specific methods
    Task<List<BreakSummary>> GetByClientIdAsync(int clientId, CancellationToken cancellationToken = default);
    
    Task<List<BreakSummary>> GetByGroupIdAsync(int groupId, bool includeSubGroups = false, CancellationToken cancellationToken = default);
    
    Task<List<BreakSummary>> GetByDateRangeAsync(DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
    
    Task<List<BreakSummary>> GetByMembershipYearAsync(int year, CancellationToken cancellationToken = default);
    
    Task<List<BreakSummary>> GetByReasonAsync(string reason, CancellationToken cancellationToken = default);
    
    Task<List<BreakSummary>> GetPaidBreaksAsync(CancellationToken cancellationToken = default);
    
    Task<List<BreakSummary>> GetUnpaidBreaksAsync(CancellationToken cancellationToken = default);
    
    Task<int> GetBreakDaysCountAsync(int clientId, int membershipYear, CancellationToken cancellationToken = default);
    
    Task<bool> HasBreakInPeriodAsync(int clientId, DateOnly startDate, DateOnly endDate, CancellationToken cancellationToken = default);
}