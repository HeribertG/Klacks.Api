// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The resource types a container edit lock may be taken on, shared by the lock endpoint, the handlers that check the
/// lock and the skills that take it. One table declares for every type what its resource id is (a container shift id or
/// a work id), so a type is either listed with its key and checked accordingly, or unknown and refused; there is no
/// second list a new type could be missing from.
/// </summary>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Constants;

public static class ContainerLockResourceTypes
{
    public const string ContainerTemplate = "ContainerTemplate";

    public const string ContainerShiftOverride = "ContainerShiftOverride";

    public const string ContainerWork = "ContainerWork";

    private static readonly IReadOnlyDictionary<string, ContainerLockKey> KeyByType =
        new Dictionary<string, ContainerLockKey>(StringComparer.Ordinal)
        {
            [ContainerTemplate] = ContainerLockKey.Container,
            [ContainerShiftOverride] = ContainerLockKey.Container,
            [ContainerWork] = ContainerLockKey.Work
        };

    /// <summary>
    /// Returns what the resource id of the given type identifies, or null for an unknown (or missing) type.
    /// </summary>
    /// <param name="resourceType">Resource type taken from the request</param>
    public static ContainerLockKey? FindKey(string? resourceType) =>
        resourceType != null && KeyByType.TryGetValue(resourceType, out var key) ? key : null;
}
