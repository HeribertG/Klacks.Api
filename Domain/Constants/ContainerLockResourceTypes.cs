// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The resource types a container edit lock may be taken on, shared by the lock endpoint, the handlers that check the
/// lock and the skills that take it. ContainerTemplate and ContainerShiftOverride locks are keyed by the container
/// shift id; a ContainerWork lock is keyed by a work id.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContainerLockResourceTypes
{
    public const string ContainerTemplate = "ContainerTemplate";

    public const string ContainerShiftOverride = "ContainerShiftOverride";

    public const string ContainerWork = "ContainerWork";

    public const string UnknownResourceType = "Unknown container lock resource type '{0}'";

    private static readonly HashSet<string> Known = new(StringComparer.Ordinal)
    {
        ContainerTemplate,
        ContainerShiftOverride,
        ContainerWork
    };

    private static readonly HashSet<string> KeyedByContainer = new(StringComparer.Ordinal)
    {
        ContainerTemplate,
        ContainerShiftOverride
    };

    public static bool IsKnown(string? resourceType) => resourceType != null && Known.Contains(resourceType);

    public static bool IsKeyedByContainer(string resourceType) => KeyedByContainer.Contains(resourceType);
}
