// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for SkillSelectionTrajectory used to capture per-turn skill selection telemetry.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
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

    // A turn the user stopped is never sharpening evidence, whatever a correction menu later says about it.
    internal IQueryable<SkillSelectionTrajectory> UncorrectedWrongSkillQuery(Guid agentId) =>
        _context.SkillSelectionTrajectories
            .Where(t => t.AgentId == agentId && t.WasCorrected && t.SharpenedAtUtc == null
                && t.CorrectionType == CorrectionTypes.WrongSkill && !t.WasInterrupted);

    public async Task<List<SkillSelectionTrajectory>> GetUncorrectedWrongSkillAsync(Guid agentId, int limit, CancellationToken cancellationToken = default)
    {
        return await UncorrectedWrongSkillQuery(agentId)
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

    // The owner is part of the predicate, not a check after the fact: a turn id that belongs to someone else
    // is indistinguishable from an unknown one, so a guessed id tells nothing about other users' turns.
    internal IQueryable<SkillSelectionTrajectory> ByUserAndTurnIdQuery(string userId, Guid turnId) =>
        _context.SkillSelectionTrajectories.Where(t => t.UserId == userId && t.TurnId == turnId);

    public async Task<SkillSelectionTrajectory?> FindByUserAndTurnIdAsync(string userId, Guid turnId, CancellationToken cancellationToken = default)
    {
        return await ByUserAndTurnIdQuery(userId, turnId)
            .OrderByDescending(t => t.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
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

    // was_successful is a snapshot taken while the trajectory was written. A UiAction reports its real
    // outcome afterwards (W1.4), flipping skill_usage_records.success without anybody rewriting that
    // snapshot - so a turn whose only execution the browser later reported as failed was still being
    // booked as a success through the "?? true" fallback. Evaluating the turn_id join here instead makes
    // the late report count. Rows without a verdict remain excluded, exactly as at capture time: "nobody has
    // reported yet" (Dispatched) and "the stop kept it from running" (Cancelled) are not failures.
    internal IQueryable<Guid> FailedTurnIds() =>
        _context.SkillUsageRecords
            .Where(u => u.TurnId != null && !u.Success)
            .Where(SkillUsagePredicates.HasVerdict)
            .Select(u => u.TurnId!.Value);

    // The three fitness queries are built apart from their execution so a unit test can pin the SQL
    // Npgsql generates for them. The sub-select is the whole point of the join and is invisible to the
    // in-memory provider: a translation that quietly stopped emitting it would keep every test green
    // while every late UiAction failure went back to counting as a success.
    internal IQueryable<UsageRow> PhraseUsageQuery(string ownerName, DateTime fromUtc)
    {
        var failedTurnIds = FailedTurnIds();

        return _context.SkillSelectionTrajectories
            .AsNoTracking()
            .Where(t => t.LearnedPhraseHit == ownerName && t.CreateTime >= fromUtc && !t.WasInterrupted)
            .Select(t => new UsageRow(
                t.CreateTime, t.WasCorrected, t.Helpful,
                t.LlmChosenSkill == ownerName && !t.WasCorrected && (t.WasSuccessful ?? true)
                    && (t.TurnId == null || !failedTurnIds.Contains(t.TurnId.Value))));
    }

    internal IQueryable<UsageRow> RecipeUsageQuery(string recipeName, DateTime fromUtc)
    {
        var failedTurnIds = FailedTurnIds();

        return _context.SkillSelectionTrajectories
            .AsNoTracking()
            .Where(t => t.RecipeName == recipeName && t.CreateTime >= fromUtc && !t.WasInterrupted)
            .Select(t => new UsageRow(
                t.CreateTime, t.WasCorrected, t.Helpful,
                !t.WasCorrected && (t.WasSuccessful ?? t.WasExecuted)
                    && (t.TurnId == null || !failedTurnIds.Contains(t.TurnId.Value))));
    }

    internal IQueryable<SkillSelectionTrajectory> SuccessfulRecipeTurnQuery(string recipeName)
    {
        var failedTurnIds = FailedTurnIds();

        return _context.SkillSelectionTrajectories
            .AsNoTracking()
            .Where(t => t.RecipeName == recipeName
                && !t.WasInterrupted
                && !t.WasCorrected
                && (t.WasSuccessful ?? t.WasExecuted)
                && (t.TurnId == null || !failedTurnIds.Contains(t.TurnId.Value)));
    }

    // Success for a phrase means the turn actually reached the skill the phrase belongs to AND every
    // skill execution of that turn succeeded (W1.3). The phrase occurring while a different skill ran
    // is a use, not a success - which is exactly the distinction the quote exists to make. Legacy rows
    // without a turn_id join keep their old semantics via the null-coalescing fallback.
    public async Task<LearnedArtefactUsage> CountPhraseUsageAsync(
        string ownerName, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        var rows = await PhraseUsageQuery(ownerName, fromUtc).ToListAsync(cancellationToken);

        return Summarise(rows);
    }

    public async Task<LearnedArtefactUsage> CountRecipeUsageAsync(
        string recipeName, DateTime fromUtc, CancellationToken cancellationToken = default)
    {
        var rows = await RecipeUsageQuery(recipeName, fromUtc).ToListAsync(cancellationToken);

        return Summarise(rows);
    }

    public async Task<bool> HasSuccessfulRecipeTurnAsync(
        string recipeName, CancellationToken cancellationToken = default)
    {
        return await SuccessfulRecipeTurnQuery(recipeName).AnyAsync(cancellationToken);
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

    internal sealed record UsageRow(DateTime? CreateTime, bool WasCorrected, bool? Helpful, bool IsSuccess);
}
