// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

/// <summary>
/// Partitions every client of the given entity type into a region/canton/city group hierarchy built
/// from their current address. With Apply=false it only previews the plan; with Apply=true it creates
/// the missing groups (reusing groups that already carry the right name under the right parent),
/// persists the memberships and re-reads them for verification.
/// </summary>
/// <param name="Level">Granularity of the partition: by canton, by city, or by canton then city.</param>
/// <param name="EntityType">Client type to partition (Employee or ExternEmp; Customer is rejected by the calling skill).</param>
/// <param name="RootGroupId">Id of an already-resolved group every top-level node attaches under; null mirrors the deterministic canton-to-region convention baked into GroupsSeed instead.</param>
/// <param name="RootGroupName">Display name of RootGroupId, carried through only to label the top-level nodes' parent in the result; null when RootGroupId is null.</param>
/// <param name="IncludeAlreadyGrouped">When false (default), clients that already hold an active group membership are skipped.</param>
/// <param name="ValidFrom">Start date of the new memberships (the plannability boundary); null defaults to today.</param>
/// <param name="Apply">False for a dry-run preview, true to create the groups and persist the memberships.</param>
/// <param name="UserName">Name of the acting user, stored on the created groups and memberships.</param>
public record PartitionClientsByAddressCommand(
    GroupPartitionLevelEnum Level,
    EntityTypeEnum EntityType,
    Guid? RootGroupId,
    string? RootGroupName,
    bool IncludeAlreadyGrouped,
    DateTime? ValidFrom,
    bool Apply,
    string UserName) : IRequest<PartitionClientsByAddressResult>;
