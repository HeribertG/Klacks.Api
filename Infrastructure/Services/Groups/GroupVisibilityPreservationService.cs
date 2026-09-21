// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Groups;

/// <summary>
/// Preserves the visibility every non-admin held before groups existed, at the moment the first group
/// is created. The baseline is resolved once per DI scope and cached, so a bulk creation asks the
/// database once and still grants visibility on every root it creates, while a later request never
/// widens anything.
/// </summary>
/// <param name="context">Reads the affected users and stages the group_visibility rows</param>
/// <param name="groupVisibility">Supplies the shared group-existence predicate and the admin list</param>
/// <param name="logger">Records how many users kept their visibility, as the audit trail of the grant</param>
public class GroupVisibilityPreservationService : IGroupVisibilityPreservationService
{
    private const string PreservedLogMessage =
        "First group created: preserved full visibility for {UserCount} user(s) on root group {GroupId}.";

    private const string NothingToPreserveLogMessage =
        "First group created, but no user needed a visibility grant for root group {GroupId}.";

    private readonly DataBaseContext _context;
    private readonly IGroupVisibilityService _groupVisibility;
    private readonly ILogger<GroupVisibilityPreservationService> _logger;

    private bool? _installationHadGroupsAtScopeStart;
    private List<string>? _affectedUserIds;

    public GroupVisibilityPreservationService(
        DataBaseContext context,
        IGroupVisibilityService groupVisibility,
        ILogger<GroupVisibilityPreservationService> logger)
    {
        _context = context;
        _groupVisibility = groupVisibility;
        _logger = logger;
    }

    public async Task<bool> RequiresPreservationAsync(Group newGroup, CancellationToken cancellationToken = default)
    {
        if (newGroup.Parent.HasValue)
        {
            return false;
        }

        return !await HadGroupsAtScopeStartAsync(cancellationToken);
    }

    public async Task<int> PreserveForNewRootAsync(Group newRoot, CancellationToken cancellationToken = default)
    {
        var userIds = await ResolveAffectedUserIdsAsync(cancellationToken);
        if (userIds.Count == 0)
        {
            _logger.LogInformation(NothingToPreserveLogMessage, newRoot.Id);
            return 0;
        }

        var alreadyVisible = await _context.GroupVisibility
            .AsNoTracking()
            .Where(x => x.GroupId == newRoot.Id)
            .Select(x => x.AppUserId)
            .ToListAsync(cancellationToken);
        var alreadyVisibleUserIds = new HashSet<string>(alreadyVisible, StringComparer.Ordinal);

        var granted = 0;
        foreach (var userId in userIds)
        {
            if (alreadyVisibleUserIds.Contains(userId))
            {
                continue;
            }

            _context.GroupVisibility.Add(new GroupVisibility
            {
                Id = Guid.NewGuid(),
                AppUserId = userId,
                Group = newRoot
            });
            granted++;
        }

        if (granted == 0)
        {
            _logger.LogInformation(NothingToPreserveLogMessage, newRoot.Id);
            return 0;
        }

        _logger.LogInformation(PreservedLogMessage, granted, newRoot.Id);
        return granted;
    }

    public async Task<int> CountUsersKeepingFullVisibilityAsync(CancellationToken cancellationToken = default)
    {
        if (await HadGroupsAtScopeStartAsync(cancellationToken))
        {
            return 0;
        }

        return (await ResolveAffectedUserIdsAsync(cancellationToken)).Count;
    }

    private async Task<bool> HadGroupsAtScopeStartAsync(CancellationToken cancellationToken)
    {
        _installationHadGroupsAtScopeStart ??= await _groupVisibility.AnyGroupsExistAsync(cancellationToken);

        return _installationHadGroupsAtScopeStart.Value;
    }

    private async Task<List<string>> ResolveAffectedUserIdsAsync(CancellationToken cancellationToken)
    {
        if (_affectedUserIds != null)
        {
            return _affectedUserIds;
        }

        var adminIds = new HashSet<string>(await _groupVisibility.ReadAdmins(), StringComparer.Ordinal);
        var activeUserIds = await _context.AppUser
            .AsNoTracking()
            .Where(u => u.DeactivatedAt == null)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        _affectedUserIds = activeUserIds.Where(id => !adminIds.Contains(id)).ToList();

        return _affectedUserIds;
    }
}
