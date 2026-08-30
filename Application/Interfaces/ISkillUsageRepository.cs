// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Interfaces;

public interface ISkillUsageRepository
{
    Task AddAsync(SkillUsageRecord record, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillUsageRecord>> GetRecordsAsync(DateTime fromDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillUsageRecord>> GetRecordsBySkillAsync(string skillName, DateTime fromDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SkillUsageRecord>> GetRecordsByUserAsync(Guid userId, DateTime fromDate, CancellationToken cancellationToken = default);
    Task<int> GetTotalExecutionsAsync(DateTime fromDate, CancellationToken cancellationToken = default);
    Task<decimal> GetSuccessRateAsync(DateTime fromDate, CancellationToken cancellationToken = default);

    /// <summary>All usage rows of one chat turn, used to derive the turn's was_successful signal (W1.3).</summary>
    Task<IReadOnlyList<SkillUsageRecord>> GetByTurnIdAsync(Guid turnId, CancellationToken cancellationToken = default);
}
