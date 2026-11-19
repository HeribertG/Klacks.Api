// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core implementation of IAppUserReader over UserManager.Users, the user set of the Identity store that shares the
/// request's DataBaseContext, so tracked results are the instances UserManager later updates.
/// </summary>
/// <param name="userManager">ASP.NET Identity manager whose Users set is read and whose e-mail normalizer is used</param>
/// <param name="context">The same request context, for the role assignments and soft-deleted identity providers</param>

using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Authentification;

public class AppUserReader : IAppUserReader
{
    private readonly UserManager<AppUser> _userManager;
    private readonly DataBaseContext _context;

    public AppUserReader(UserManager<AppUser> userManager, DataBaseContext context)
    {
        _userManager = userManager;
        _context = context;
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

    public Task<AppUser?> FindByDirectoryLinkAsync(Guid identityProviderId, string directoryExternalId, CancellationToken cancellationToken = default) =>
        _userManager.Users
            .FirstOrDefaultAsync(
                u => u.IdentityProviderId == identityProviderId && u.DirectoryExternalId == directoryExternalId,
                cancellationToken);

    public Task<List<AppUser>> GetByIdentityProviderAsync(Guid identityProviderId, CancellationToken cancellationToken = default) =>
        _userManager.Users
            .AsNoTracking()
            .Where(u => u.IdentityProviderId == identityProviderId)
            .OrderBy(u => u.DisplayOrder)
            .ToListAsync(cancellationToken);

    public async Task<HashSet<string>> GetEmailsInUseAsync(IReadOnlyCollection<string> emails, CancellationToken cancellationToken = default)
    {
        var byNormalized = emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => (Email: email, Normalized: _userManager.NormalizeEmail(email)))
            .Where(pair => pair.Normalized != null)
            .GroupBy(pair => pair.Normalized!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(pair => pair.Email).ToList(), StringComparer.Ordinal);

        var inUse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (byNormalized.Count == 0)
        {
            return inUse;
        }

        var normalizedEmails = byNormalized.Keys.ToList();
        var used = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.NormalizedEmail != null && normalizedEmails.Contains(u.NormalizedEmail))
            .Select(u => u.NormalizedEmail!)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var normalized in used)
        {
            if (byNormalized.TryGetValue(normalized, out var spellings))
            {
                inUse.UnionWith(spellings);
            }
        }

        return inUse;
    }

    public async Task<HashSet<string>> GetUserIdsWithRolesAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var ids = userIds.ToList();
        var withRoles = await _context.UserRoles
            .AsNoTracking()
            .Where(userRole => ids.Contains(userRole.UserId))
            .Select(userRole => userRole.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return new HashSet<string>(withRoles, StringComparer.Ordinal);
    }

    public async Task<AppUser?> FindLinkedToDeletedProviderAsync(string directoryExternalId, CancellationToken cancellationToken = default)
    {
        var deletedProviderIds = _context.IdentityProviders
            .IgnoreQueryFilters()
            .Where(provider => provider.IsDeleted)
            .Select(provider => (Guid?)provider.Id);

        var candidates = await _userManager.Users
            .Where(u => u.DirectoryExternalId == directoryExternalId && deletedProviderIds.Contains(u.IdentityProviderId))
            .OrderBy(u => u.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        return candidates.Count == 1 ? candidates[0] : null;
    }
}
