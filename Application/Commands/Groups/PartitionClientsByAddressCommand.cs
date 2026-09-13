// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

/// <summary>
/// Partitions every client of the given entity type(s) into a region/state/city group hierarchy built
/// from their current address. With Apply=false it only previews the plan; with Apply=true it creates
/// the missing groups (reusing groups that already carry the right name under the right parent),
/// persists the memberships and re-reads them for verification.
/// </summary>
/// <param name="Level">Granularity of the partition: by cluster, by state, by city, or by state then city.</param>
/// <param name="EntityTypes">Client types to partition; several types share one tree.</param>
/// <param name="RootGroupId">Id of an already-resolved group every top-level node attaches under; null mirrors the deterministic state-to-region convention baked into GroupsSeed instead.</param>
/// <param name="RootGroupName">Display name of RootGroupId, carried through only to label the top-level nodes' parent in the result; null when RootGroupId is null.</param>
/// <param name="IncludeAlreadyGrouped">When false (default), clients that already hold an active group membership are skipped.</param>
/// <param name="ValidFrom">Start date of the new memberships (the plannability boundary); null defaults to today.</param>
/// <param name="Apply">False for a dry-run preview, true to create the groups and persist the memberships.</param>
/// <param name="UserName">Name of the acting user, stored on the created groups and memberships.</param>
/// <param name="ClusterSharePercent">Minimum share (1-100) a city needs to become a cluster center; only used at Cluster level.</param>
public record PartitionClientsByAddressCommand(
    GroupPartitionLevelEnum Level,
    IReadOnlyList<EntityTypeEnum> EntityTypes,
    Guid? RootGroupId,
    string? RootGroupName,
    bool IncludeAlreadyGrouped,
    DateTime? ValidFrom,
    bool Apply,
    string UserName,
    int ClusterSharePercent) : IRequest<PartitionClientsByAddressResult>;
