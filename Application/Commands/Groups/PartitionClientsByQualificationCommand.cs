// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

/// <summary>
/// Creates one group per qualification held today by enough clients and adds the holders to it. With
/// Apply=false it only previews the plan; with Apply=true it creates the missing groups top-down (reusing groups
/// with the same name under the same parent), persists the new memberships and re-reads them for verification.
/// </summary>
/// <param name="EntityTypes">Client types to consider; several types share one set of groups.</param>
/// <param name="ScopeGroupId">Already-resolved group the qualification groups go under; only members of it or its subtree are considered. Null puts the groups under the localized qualifications root group and considers every client.</param>
/// <param name="ScopeGroupName">Display name of ScopeGroupId; null when ScopeGroupId is null.</param>
/// <param name="MinMembers">Minimum number of holders a qualification needs to get a group; values below MinimumMinMembers count as MinimumMinMembers (EffectiveMinMembers).</param>
/// <param name="IncludeAlreadyGrouped">When false, clients with a current membership outside the scope subtree (without a scope: any membership) are skipped.</param>
/// <param name="ValidFrom">Start date of the new memberships and groups; null defaults to today.</param>
/// <param name="Apply">False for a dry-run preview, true to create the groups and persist the memberships.</param>
/// <param name="UserName">Name of the acting user, stored on the created groups and memberships.</param>
public record PartitionClientsByQualificationCommand(
    IReadOnlyList<EntityTypeEnum> EntityTypes,
    Guid? ScopeGroupId,
    string? ScopeGroupName,
    int MinMembers,
    bool IncludeAlreadyGrouped,
    DateTime? ValidFrom,
    bool Apply,
    string UserName) : IRequest<PartitionClientsByQualificationResult>
{
    public const int MinimumMinMembers = 1;

    public const int DefaultMinMembers = 2;

    public const string AllEntityTypesLabel = "All";

    public int EffectiveMinMembers => Math.Max(MinimumMinMembers, MinMembers);
}
