// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF-backed implementation of IUserIntroTourStateRepository on table user_intro_tour_states.
/// </summary>
/// <param name="context">Shared request-scoped database context</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class UserIntroTourStateRepository : IUserIntroTourStateRepository
{
    private static readonly string EnsureExistsSql =
        $"INSERT INTO {IntroTourDefaults.TableName} (user_id, status, completed_stations, snooze_count, create_time) "
        + "VALUES ({0}, {1}, '[]'::jsonb, 0, {2}) ON CONFLICT (user_id) DO NOTHING";

    private readonly DataBaseContext _context;

    public UserIntroTourStateRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<UserIntroTourState?> GetNoTrackingAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserIntroTourStates
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task<UserIntroTourState?> GetForUpdateAsync(string userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserIntroTourStates
            .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);
    }

    public async Task EnsureExistsAsync(string userId, string initialStatus, DateTime nowUtc, CancellationToken cancellationToken = default)
    {
        await _context.Database.ExecuteSqlRawAsync(
            EnsureExistsSql,
            new object[] { userId, initialStatus, nowUtc },
            cancellationToken);
    }
}
