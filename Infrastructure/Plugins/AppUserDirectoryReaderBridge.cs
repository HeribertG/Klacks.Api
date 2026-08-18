// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bridges the Contracts IAppUserDirectoryReader to the Identity AppUser table.
/// Excludes deactivated accounts - a deactivated user must not be reachable through a plugin
/// invite any more than through the regular admin user list.
/// </summary>
/// <param name="context">EF Core database context.</param>

using Klacks.Api.Infrastructure.Persistence;
using Klacks.Plugin.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Plugins;

public class AppUserDirectoryReaderBridge : IAppUserDirectoryReader
{
    private readonly DataBaseContext _context;

    public AppUserDirectoryReaderBridge(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<AppUserDirectoryInfo?> GetUserAsync(string userId, CancellationToken ct = default)
    {
        var user = await _context.AppUser
            .AsNoTracking()
            .Where(u => u.Id == userId && u.DeactivatedAt == null)
            .Select(u => new { u.Id, u.FirstName, u.Email })
            .FirstOrDefaultAsync(ct);

        return user == null ? null : new AppUserDirectoryInfo(user.Id, user.FirstName, user.Email);
    }
}
