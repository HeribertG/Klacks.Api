// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class RecipeRunRepository : IRecipeRunRepository
{
    private readonly DataBaseContext _context;

    public RecipeRunRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<RecipeRun?> FindRunningAsync(
        Guid userId, string conversationId, string recipeName, CancellationToken cancellationToken = default)
    {
        return await _context.RecipeRuns
            .Where(r => r.UserId == userId
                && r.ConversationId == conversationId
                && r.RecipeName == recipeName
                && r.Status == RecipeRunStatus.Running)
            .OrderByDescending(r => r.CreateTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<RecipeRun?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.RecipeRuns
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddAsync(RecipeRun run, CancellationToken cancellationToken = default)
    {
        await _context.RecipeRuns.AddAsync(run, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(RecipeRun run, CancellationToken cancellationToken = default)
    {
        _context.RecipeRuns.Update(run);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ExpireStaleAsync(
        DateTime olderThanUtc, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        return await _context.RecipeRuns
            .Where(r => r.Status == RecipeRunStatus.Running
                && r.UpdateTime != null
                && r.UpdateTime < olderThanUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, RecipeRunStatus.Expired)
                    .SetProperty(r => r.UpdateTime, nowUtc),
                cancellationToken);
    }

    public async Task<int> ExpireStaleRunsAsync(
        Guid userId, string conversationId, DateTime olderThanUtc, CancellationToken cancellationToken = default)
    {
        return await _context.RecipeRuns
            .Where(r => r.UserId == userId
                && r.ConversationId == conversationId
                && r.Status == RecipeRunStatus.Running
                && r.UpdateTime != null
                && r.UpdateTime < olderThanUtc)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, RecipeRunStatus.Expired)
                    .SetProperty(r => r.UpdateTime, DateTime.UtcNow),
                cancellationToken);
    }
}
