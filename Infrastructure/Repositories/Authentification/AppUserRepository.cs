// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core implementation of IAppUserRepository; writes AppUser.DisplayOrder and commits immediately.
/// </summary>
/// <param name="context">Scoped database context shared with the other repositories of the request</param>

using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Authentification;

public class AppUserRepository : IAppUserRepository
{
    private const int FirstDisplayPosition = 1;

    private readonly DataBaseContext _context;

    public AppUserRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task ApplyDisplayOrderAsync(IReadOnlyList<string> orderedUserIds, CancellationToken cancellationToken = default)
    {
        var users = await _context.AppUser
            .Where(u => orderedUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var position = FirstDisplayPosition;
        foreach (var userId in orderedUserIds)
        {
            if (users.TryGetValue(userId, out var user))
            {
                user.DisplayOrder = position;
            }

            position++;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
