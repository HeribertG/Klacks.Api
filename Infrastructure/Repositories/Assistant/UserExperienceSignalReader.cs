// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the usage facts the explanation-depth heuristic needs for one user: the live real-plan works the user
/// created (audit stamp CurrentUserCreated holds the user id string, see DataBaseContext.CurrentAuditActorName) with
/// the span between the oldest and the newest, and the user's successful Klacksy skill writes (category Crud or Action).
/// </summary>
/// <param name="context">Database context with works and skill usage records</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public sealed class UserExperienceSignalReader : IUserExperienceSignalReader
{
    private readonly DataBaseContext _context;

    public UserExperienceSignalReader(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<UserExperienceSignals> ReadAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var auditName = userId.ToString();

        var works = await _context.Work
            .AsNoTracking()
            .Where(w => w.CurrentUserCreated == auditName && !w.IsDeleted && w.AnalyseToken == null)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Count = g.Count(),
                First = g.Min(w => w.CreateTime),
                Last = g.Max(w => w.CreateTime)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var skillWrites = await _context.SkillUsageRecords
            .AsNoTracking()
            .CountAsync(
                r => r.UserId == userId
                     && r.Success
                     && !r.IsDeleted
                     && (r.Category == SkillCategory.Crud || r.Category == SkillCategory.Action),
                cancellationToken);

        return new UserExperienceSignals(works?.Count ?? 0, works?.First, works?.Last, skillWrites);
    }
}
