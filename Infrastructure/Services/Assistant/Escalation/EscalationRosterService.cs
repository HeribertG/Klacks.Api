// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Re-derives EscalationRosterEntry.DerivedRank from GroupVisibility (ordered by CreateTime, the
/// "index" the Entwurf uses as a re-derivable starting point) and combines it with the admin
/// OverrideRank correction, then appends the global admin role as the trailing stage (A2). Nested
/// Set has multiple roots, so every lookup is keyed by Group.Root ?? groupId, mirroring the same
/// resolution FindReplacementQueryHandler performs for its own candidate pool.
/// Simplification, documented rather than hidden: visibility is read at the group ROOT only, not
/// unioned across subgroups, so a planner with visibility scoped to a subgroup will not appear here
/// until the admin grants visibility on the root itself.
/// </summary>
/// <param name="context">Direct EF access for GroupVisibility and EscalationRosterEntry: no separate
/// repository exists for either read shape this service needs.</param>
/// <param name="groupRepository">Resolves a group to its Nested Set root.</param>
/// <param name="userManager">Resolves display names and the global admin role membership.</param>
/// <param name="logger">Logs an unresolvable roster user id (deleted AppUser row survived by a stale GroupVisibility).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Escalation;
using Klacks.Api.Domain.Models.Authentification;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Assistant.Escalation;

public class EscalationRosterService : IEscalationRosterService
{
    private readonly DataBaseContext _context;
    private readonly IGroupRepository _groupRepository;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<EscalationRosterService> _logger;

    public EscalationRosterService(
        DataBaseContext context,
        IGroupRepository groupRepository,
        UserManager<AppUser> userManager,
        ILogger<EscalationRosterService> logger)
    {
        _context = context;
        _groupRepository = groupRepository;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EscalationRosterCandidate>> GetOrderedRosterAsync(
        Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await _groupRepository.Get(groupId);
        var rootId = group?.Root ?? groupId;

        var orderedUserIds = await RederiveAsync(rootId, cancellationToken);

        var candidates = new List<EscalationRosterCandidate>();
        var seenUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var userId in orderedUserIds)
        {
            var candidate = await ResolveCandidateAsync(userId);
            if (candidate is null)
            {
                continue;
            }

            candidates.Add(candidate.Value);
            seenUserIds.Add(userId);
        }

        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
        foreach (var admin in admins.OrderBy(a => a.LastName).ThenBy(a => a.FirstName))
        {
            if (seenUserIds.Contains(admin.Id))
            {
                continue;
            }

            candidates.Add(new EscalationRosterCandidate(admin.Id, BuildDisplayName(admin)));
            seenUserIds.Add(admin.Id);
        }

        return candidates;
    }

    /// <summary>
    /// Upserts EscalationRosterEntry rows for the current GroupVisibility membership and returns the
    /// resulting user ids in effective-rank order. A visible user without a row gets one; a row whose
    /// user lost visibility is deleted unless an admin OverrideRank exists, in which case it is
    /// marked IsOrphaned instead (E60) and excluded from the returned order.
    /// </summary>
    private async Task<IReadOnlyList<string>> RederiveAsync(Guid rootId, CancellationToken cancellationToken)
    {
        var visibleUserIds = await _context.GroupVisibility
            .Where(v => v.GroupId == rootId)
            .OrderBy(v => v.CreateTime)
            .Select(v => v.AppUserId)
            .ToListAsync(cancellationToken);

        var existingEntries = await _context.Set<EscalationRosterEntry>()
            .Where(r => r.GroupRootId == rootId)
            .ToDictionaryAsync(r => r.UserId, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var visibleSet = new HashSet<string>(visibleUserIds, StringComparer.OrdinalIgnoreCase);
        var rank = 1;
        foreach (var userId in visibleUserIds)
        {
            if (existingEntries.TryGetValue(userId, out var entry))
            {
                entry.DerivedRank = rank;
                entry.IsOrphaned = false;
            }
            else
            {
                _context.Set<EscalationRosterEntry>().Add(new EscalationRosterEntry
                {
                    Id = Guid.NewGuid(),
                    GroupRootId = rootId,
                    UserId = userId,
                    DerivedRank = rank,
                    IsOrphaned = false
                });
            }

            rank++;
        }

        foreach (var entry in existingEntries.Values)
        {
            if (visibleSet.Contains(entry.UserId))
            {
                continue;
            }

            if (entry.OverrideRank.HasValue)
            {
                entry.IsOrphaned = true;
                entry.DerivedRank = null;
            }
            else
            {
                _context.Set<EscalationRosterEntry>().Remove(entry);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _context.Set<EscalationRosterEntry>()
            .Where(r => r.GroupRootId == rootId && !r.IsOrphaned)
            .OrderBy(r => r.OverrideRank ?? r.DerivedRank)
            .Select(r => r.UserId)
            .ToListAsync(cancellationToken);
    }

    private async Task<EscalationRosterCandidate?> ResolveCandidateAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning(
                "Escalation roster references user {UserId} who no longer exists; skipping this rank.", userId);
            return null;
        }

        return new EscalationRosterCandidate(user.Id, BuildDisplayName(user));
    }

    private static string BuildDisplayName(AppUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return name.Length > 0 ? name : (user.UserName ?? user.Id);
    }
}
