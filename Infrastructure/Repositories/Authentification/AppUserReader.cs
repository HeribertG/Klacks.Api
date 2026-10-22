// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core implementation of IAppUserReader over UserManager.Users, the user set of the Identity store that shares the
/// request's DataBaseContext, so tracked results are the instances UserManager later updates.
/// </summary>
/// <param name="userManager">ASP.NET Identity manager whose Users set is read</param>

using Klacks.Api.Domain.Models.Authentification;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Authentification;

public class AppUserReader : IAppUserReader
{
    private readonly UserManager<AppUser> _userManager;

    public AppUserReader(UserManager<AppUser> userManager)
    {
        _userManager = userManager;
    }

    public Task<List<AppUser>> GetActiveUsersInDisplayOrderAsync(CancellationToken cancellationToken = default) =>
        _userManager.Users
            .Where(u => u.DeactivatedAt == null)
            .OrderBy(u => u.DisplayOrder)
            .ToListAsync(cancellationToken);

    public Task<AppUser?> FindByPasswordResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        _userManager.Users
            .FirstOrDefaultAsync(u => u.PasswordResetToken == tokenHash, cancellationToken);

    public Task<AppUser?> FindByIdNoTrackingAsync(string userId, CancellationToken cancellationToken = default) =>
        _userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
}
