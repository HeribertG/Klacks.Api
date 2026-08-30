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

    /// <summary>Single usage row by id; the UiAction report endpoint resolves the tracking id through it.</summary>
    Task<SkillUsageRecord?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a usage-row change (W1.4 frontend outcome report).</summary>
    Task UpdateAsync(SkillUsageRecord record, CancellationToken cancellationToken = default);
}
