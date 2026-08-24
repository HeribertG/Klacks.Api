// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads one entity's group set out of a batched IShiftGroupScopeReader result. The reader omits keys
/// with no live membership rather than mapping them to an empty list, and every detector needs the
/// same "absent means no group" translation before it can hand the set to a trigger event.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public static class ShiftGroupScope
{
    public static IReadOnlyCollection<Guid> For(
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> groupsByKey,
        Guid key) =>
        groupsByKey.TryGetValue(key, out var groupIds) ? groupIds : Array.Empty<Guid>();
}
