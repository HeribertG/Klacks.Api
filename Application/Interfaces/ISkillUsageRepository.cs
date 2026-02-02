using Klacks.Api.Domain.Models.Skills;

namespace Klacks.Api.Application.Interfaces;

public interface ISkillUsageRepository
{
    Task AddAsync(SkillUsageRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillUsageRecord>> GetRecordsAsync(DateTime fromDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillUsageRecord>> GetRecordsBySkillAsync(string skillName, DateTime fromDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillUsageRecord>> GetRecordsByUserAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default);
    Task<int> GetTotalExecutionsAsync(DateTime fromDate, CancellationToken cancellationToken = default);
    Task<decimal> GetSuccessRateAsync(DateTime fromDate, CancellationToken cancellationToken = default);
}
