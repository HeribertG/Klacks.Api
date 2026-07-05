// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Associations;

public record GroupVisibilityScope(
    bool IsUnrestricted,
    IReadOnlyList<Guid> VisibleRootIds,
    IReadOnlyList<Guid> VisibleGroupIds)
{
    public static GroupVisibilityScope Unrestricted() => new(true, [], []);

    public static GroupVisibilityScope Restricted(IReadOnlyList<Guid> visibleRootIds, IReadOnlyList<Guid> visibleGroupIds) =>
        new(false, visibleRootIds, visibleGroupIds);

    public bool HasVisibleGroups => IsUnrestricted || VisibleRootIds.Count > 0;
}
