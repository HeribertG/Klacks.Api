// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolved group scope of the user behind a skill execution. Unrestricted access passes every
/// group through; restricted access allows only groups whose root belongs to the user's visible
/// root groups (the whole subtree of an assigned root is in scope).
/// </summary>
/// <param name="visibleRootIds">Ids of the root groups the caller may act on (empty when unrestricted).</param>
/// <param name="visibleRootNames">Display names of the visible root groups, used in error messages.</param>

namespace Klacks.Api.Domain.Models.Associations;

public sealed class GroupScopeAccess
{
    private const string OutOfScopeTemplate =
        "Group '{0}' is outside your assigned group scope. You can only work with groups under: {1}. " +
        "Pick one of these groups or ask an administrator to extend your group scope.";

    private const string NoRootScopeTemplate =
        "Creating a root-level group is outside your assigned group scope. You can only work with " +
        "groups under: {0}. Create the group under one of these parents or ask an administrator.";

    private readonly HashSet<Guid> _visibleRootIds;

    private GroupScopeAccess(
        bool isUnrestricted,
        IEnumerable<Guid> visibleRootIds,
        IEnumerable<string> visibleRootNames)
    {
        IsUnrestricted = isUnrestricted;
        _visibleRootIds = visibleRootIds.ToHashSet();
        VisibleRootNames = visibleRootNames.ToList();
    }

    public bool IsUnrestricted { get; }

    public IReadOnlyList<string> VisibleRootNames { get; }

    public static GroupScopeAccess Unrestricted() => new(true, [], []);

    public static GroupScopeAccess Restricted(
        IEnumerable<Guid> visibleRootIds, IEnumerable<string> visibleRootNames) =>
        new(false, visibleRootIds, visibleRootNames);

    public bool IsInScope(Group group) =>
        IsUnrestricted || _visibleRootIds.Contains(group.Root ?? group.Id);

    public IReadOnlyList<Group> Filter(IEnumerable<Group> groups) =>
        IsUnrestricted ? groups.ToList() : groups.Where(IsInScope).ToList();

    public string BuildOutOfScopeError(string groupName) =>
        string.Format(OutOfScopeTemplate, groupName, JoinRootNames());

    public string BuildRootCreationError() =>
        string.Format(NoRootScopeTemplate, JoinRootNames());

    private string JoinRootNames() => string.Join(", ", VisibleRootNames);
}
