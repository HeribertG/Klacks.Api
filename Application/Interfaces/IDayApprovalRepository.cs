using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IDayApprovalRepository : IBaseRepository<DayApproval>
{
    Task<DayApproval?> GetByDateAndGroup(DateOnly date, Guid groupId);
    Task<List<DayApproval>> GetByDateRange(DateOnly startDate, DateOnly endDate, Guid? groupId = null);
    Task<bool> ExistsForDateAndGroup(DateOnly date, Guid groupId);
}
