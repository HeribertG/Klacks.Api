// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Everything QualificationGroupPlanner needs besides the clients, qualifications and groups, resolved by the
/// handler so the planner stays a pure function.
/// </summary>
/// <param name="NameLanguages">Languages tried in order to name a qualification group (installation language first, then its base language, then the core languages).</param>
/// <param name="Today">Company-local date that decides which qualifications and memberships are current.</param>
/// <param name="ParentGroupId">Existing group the qualification groups go under; null while the parent is still to be created (then every group is new).</param>
/// <param name="ScopeGroupIds">The scope group and its whole subtree; only clients with a current membership in one of them are considered. Null considers every client.</param>
/// <param name="MinMembers">Minimum number of considered clients a qualification needs to get a group (at least 1).</param>
/// <param name="IncludeAlreadyGrouped">When false, clients with a current membership outside OwnSubtreeGroupIds are skipped.</param>
/// <param name="OwnSubtreeGroupIds">The parent group and its whole subtree (the scope subtree, or an existing qualifications root with its qualification groups); memberships there never count as "already grouped", so a re-run keeps its own members. Null when the parent does not exist yet.</param>
public sealed record QualificationGroupPlanContext(
    IReadOnlyList<string> NameLanguages,
    DateOnly Today,
    Guid? ParentGroupId,
    IReadOnlySet<Guid>? ScopeGroupIds,
    int MinMembers,
    bool IncludeAlreadyGrouped,
    IReadOnlySet<Guid>? OwnSubtreeGroupIds = null);
