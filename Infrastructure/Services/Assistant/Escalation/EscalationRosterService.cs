// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a group's escalation call list from GroupVisibility, UserAbsencePeriod and
/// AppUser.EscalationRosterOrder - a column dedicated to this domain, never shared with the user
/// administration list's DisplayOrder. Nested Set has multiple roots, so GetOrderedRosterAsync keys
/// every lookup by Group.Root ?? groupId, mirroring the same resolution FindReplacementQueryHandler
/// performs for its own candidate pool. GetRosterMembersAsync/ReorderAsync are intentionally NOT
/// group-scoped: one flat, admin-facing list of every user with any GroupVisibility and a phone
/// number (Owner decision, 17.08.) - which group(s) a member actually gets called for is decided by
/// their own GroupVisibility rows at escalation time, not by anything picked on this page.
/// Simplification, documented rather than hidden: GetOrderedRosterAsync reads visibility at the group
/// ROOT only, not unioned across subgroups, so a planner with visibility scoped to a subgroup will not
/// appear here until the admin grants visibility on the root itself.
/// </summary>
/// <param name="context">Direct EF access for GroupVisibility, UserAbsencePeriod and AppUser: no
/// separate repository exists for any of these read/write shapes.</param>
/// <param name="groupRepository">Resolves a group to its Nested Set root.</param>
/// <param name="userManager">Resolves display names and the global admin role membership.</param>
/// <param name="timeProvider">Injected clock so a test can control "today" for absence filtering.</param>
/// <param name="logger">Reserved for future diagnostics; no warning path currently needs it.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs;
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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EscalationRosterService> _logger;

    public EscalationRosterService(
        DataBaseContext context,
        IGroupRepository groupRepository,
        UserManager<AppUser> userManager,
        TimeProvider timeProvider,
        ILogger<EscalationRosterService> logger)
    {
        _context = context;
        _groupRepository = groupRepository;
        _userManager = userManager;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<EscalationRosterCandidate>> GetOrderedRosterAsync(
        Guid groupId, CancellationToken cancellationToken = default)
    {
        var rootId = await ResolveRootAsync(groupId);
        var visibleUserIds = await GetVisibleUserIdsAsync(rootId, cancellationToken);
        var admins = await _userManager.GetUsersInRoleAsync(Roles.Admin);
        var absentUserIds = await GetCurrentlyAbsentUserIdsAsync(cancellationToken);

        var visibleUsers = await _context.AppUser
            .Where(u => visibleUserIds.Contains(u.Id))
            .OrderBy(u => u.EscalationRosterOrder)
            .ToListAsync(cancellationToken);

        var candidates = new List<EscalationRosterCandidate>();
        var seenUserIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var user in visibleUsers)
        {
            if (!IsReachable(user, absentUserIds))
            {
                continue;
            }

            candidates.Add(new EscalationRosterCandidate(user.Id, BuildDisplayName(user)));
            seenUserIds.Add(user.Id);
        }

        // The A2 fallback stage: every admin not already present above, appended last, in the same
        // EscalationRosterOrder as everywhere else. Deliberately not exempt from the absence/phone
        // filters (Owner decision, 17.08.): an unreachable admin is still unreachable.
        foreach (var admin in admins.OrderBy(a => a.EscalationRosterOrder))
        {
            if (seenUserIds.Contains(admin.Id) || !IsReachable(admin, absentUserIds))
            {
                continue;
            }

            candidates.Add(new EscalationRosterCandidate(admin.Id, BuildDisplayName(admin)));
            seenUserIds.Add(admin.Id);
        }

        return candidates;
    }

    public async Task<IReadOnlyList<EscalationRosterMember>> GetRosterMembersAsync(
        CancellationToken cancellationToken = default)
    {
        var eligibleUserIds = await GetEligibleUserIdsAsync(cancellationToken);
        var absentUserIds = await GetCurrentlyAbsentUserIdsAsync(cancellationToken);

        var members = await _context.AppUser
            .Where(u => eligibleUserIds.Contains(u.Id))
            .OrderBy(u => u.EscalationRosterOrder)
            .ToListAsync(cancellationToken);

        return members
            .Select(u => new EscalationRosterMember(
                u.Id,
                BuildDisplayName(u),
                absentUserIds.Contains(u.Id)))
            .ToList();
    }

    public async Task<HttpResultResource> ReorderAsync(
        IReadOnlyList<string> orderedUserIds, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Reordering {Count} escalation roster members", orderedUserIds.Count);

        var users = await _context.AppUser
            .Where(u => orderedUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var position = 1;
        foreach (var userId in orderedUserIds)
        {
            if (users.TryGetValue(userId, out var user))
            {
                user.EscalationRosterOrder = position;
            }

            position++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new HttpResultResource { Success = true, Messages = "Escalation roster order updated" };
    }

    /// <summary>Union of every user with at least one GroupVisibility row, restricted to those who
    /// have a phone number (Owner decision, 17.08.: a phone-less user can never be part of an
    /// escalation call, so they do not belong in the admin's editable roster either). Admins are
    /// included only if they meet the same criteria themselves - otherwise they remain the invisible
    /// A2 fallback used by GetOrderedRosterAsync.</summary>
    private async Task<HashSet<string>> GetEligibleUserIdsAsync(CancellationToken cancellationToken)
    {
        var visibleUserIds = await _context.GroupVisibility
            .Select(v => v.AppUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var eligibleIds = await _context.AppUser
            .Where(u => visibleUserIds.Contains(u.Id) && u.PhoneNumber != null && u.PhoneNumber != string.Empty)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(eligibleIds, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Guid> ResolveRootAsync(Guid groupId)
    {
        var group = await _groupRepository.Get(groupId);
        return group?.Root ?? groupId;
    }

    private async Task<HashSet<string>> GetVisibleUserIdsAsync(Guid rootId, CancellationToken cancellationToken)
    {
        var ids = await _context.GroupVisibility
            .Where(v => v.GroupId == rootId)
            .Select(v => v.AppUserId)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> GetCurrentlyAbsentUserIdsAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        var ids = await _context.UserAbsencePeriod
            .Where(a => a.StartDate <= today && a.EndDate >= today)
            .Select(a => a.AppUserId)
            .ToListAsync(cancellationToken);

        return new HashSet<string>(ids, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsReachable(AppUser user, HashSet<string> absentUserIds)
    {
        return !absentUserIds.Contains(user.Id) && !string.IsNullOrWhiteSpace(user.PhoneNumber);
    }

    private static string BuildDisplayName(AppUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        var userName = user.UserName ?? user.Id;
        return name.Length > 0 ? $"{name} ({userName})" : userName;
    }
}
