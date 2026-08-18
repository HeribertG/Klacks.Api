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
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .FirstOrDefaultAsync(ct);

        return user == null ? null : new AppUserDirectoryInfo(user.Id, user.FirstName, user.LastName, user.Email);
    }

    public async Task<IReadOnlyList<AppUserDirectoryInfo>> SearchByNameAsync(string nameQuery, CancellationToken ct = default)
    {
        var keywords = nameQuery.Trim().ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (keywords.Length == 0)
            return Array.Empty<AppUserDirectoryInfo>();

        var query = _context.AppUser.AsNoTracking().Where(u => u.DeactivatedAt == null);

        foreach (var keyword in keywords)
        {
            query = query.Where(u =>
                EF.Functions.ILike(u.FirstName, $"%{keyword}%")
                || EF.Functions.ILike(u.LastName, $"%{keyword}%")
                || (u.UserName != null && EF.Functions.ILike(u.UserName, $"%{keyword}%"))
                || (u.Email != null && EF.Functions.ILike(u.Email, $"%{keyword}%")));
        }

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Take(5)
            .Select(u => new { u.Id, u.FirstName, u.LastName, u.Email })
            .ToListAsync(ct);

        return users
            .Select(u => new AppUserDirectoryInfo(u.Id, u.FirstName, u.LastName, u.Email))
            .ToList();
    }
}
