// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for SkillSelectionTrajectory used to capture per-turn skill selection telemetry.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class SkillSelectionTrajectoryRepository : ISkillSelectionTrajectoryRepository
{
    private readonly DataBaseContext _context;

    public SkillSelectionTrajectoryRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default)
    {
        await _context.SkillSelectionTrajectories.AddAsync(record, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SkillSelectionTrajectory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task UpdateAsync(SkillSelectionTrajectory record, CancellationToken cancellationToken = default)
    {
        _context.SkillSelectionTrajectories.Update(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<SkillSelectionTrajectory>> GetRecentAsync(Guid agentId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .Where(t => t.AgentId == agentId)
            .OrderByDescending(t => t.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<SkillSelectionTrajectory>> GetUncorrectedWrongSkillAsync(Guid agentId, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .Where(t => t.AgentId == agentId && t.WasCorrected && t.SharpenedAtUtc == null
                && t.CorrectionType == CorrectionTypes.WrongSkill)
            .OrderByDescending(t => t.CreateTime)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkSharpenedAsync(
        IReadOnlyList<Guid> ids, DateTime sharpenedAtUtc, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await _context.SkillSelectionTrajectories
            .Where(t => ids.Contains(t.Id) && t.SharpenedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(t => t.SharpenedAtUtc, sharpenedAtUtc)
                    .SetProperty(t => t.UpdateTime, sharpenedAtUtc),
                cancellationToken);
    }

    public async Task<SkillSelectionTrajectory?> FindMostRecentByUserAndHashAsync(string userId, string userMessageHash, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .Where(t => t.UserId == userId && t.UserMessageHash == userMessageHash)
            .OrderByDescending(t => t.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<SkillSelectionTrajectory?> FindMostRecentByAgentAndUserAsync(Guid agentId, string userId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .Where(t => t.AgentId == agentId && t.UserId == userId)
            .OrderByDescending(t => t.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Success for a phrase means the turn actually reached the skill the phrase belongs to. The phrase
    // occurring while a different skill ran is a use, not a success - which is exactly the distinction
    // the quote exists to make.
    public async Task<LearnedArtefactUsage> CountPhraseUsageAsync(
        string ownerName, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SkillSelectionTrajectories
            .AsNoTracking()
            .Where(t => t.LearnedPhraseHit == ownerName && t.CreateTime >= fromUtc)
            .Select(t => new UsageRow(
                t.CreateTime, t.WasCorrected, t.Helpful, t.LlmChosenSkill == ownerName && !t.WasCorrected))
            .ToListAsync(cancellationToken);

        return Summarise(rows);
    }

    public async Task<LearnedArtefactUsage> CountRecipeUsageAsync(
        string recipeName, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        var rows = await _context.SkillSelectionTrajectories
            .AsNoTracking()
            .Where(t => t.RecipeName == recipeName && t.CreateTime >= fromUtc)
            .Select(t => new UsageRow(t.CreateTime, t.WasCorrected, t.Helpful, t.WasExecuted && !t.WasCorrected))
            .ToListAsync(cancellationToken);

        return Summarise(rows);
    }

    public async Task<bool> HasSuccessfulRecipeTurnAsync(
        string recipeName, CancellationToken cancellationToken = default)
    {
        return await _context.SkillSelectionTrajectories
            .AsNoTracking()
            .AnyAsync(t => t.RecipeName == recipeName && t.WasExecuted && !t.WasCorrected, cancellationToken);
    }

    private static LearnedArtefactUsage Summarise(IReadOnlyList<UsageRow> rows) =>
        rows.Count == 0
            ? LearnedArtefactUsage.None
            : new LearnedArtefactUsage(
                rows.Count,
                rows.Count(row => row.IsSuccess),
                rows.Count(row => row.WasCorrected),
                rows.Count(row => row.Helpful == true),
                rows.Max(row => row.CreateTime));

    private sealed record UsageRow(DateTime? CreateTime, bool WasCorrected, bool? Helpful, bool IsSuccess);
}
