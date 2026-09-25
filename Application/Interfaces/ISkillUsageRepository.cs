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

    /// <summary>
    /// Closes the UiAction rows of a stopped turn: every row of the turn still in Dispatched state becomes
    /// Cancelled and unsuccessful, because the client never received its steps. Rows of other turns and rows
    /// that already carry an outcome are left alone. Self-committing.
    /// </summary>
    /// <param name="turnId">The stopped turn</param>
    /// <param name="cancellationToken">Cancels the write</param>
    /// <returns>The number of rows closed</returns>
    Task<int> CancelDispatchedForTurnAsync(Guid turnId, CancellationToken cancellationToken = default);
}
