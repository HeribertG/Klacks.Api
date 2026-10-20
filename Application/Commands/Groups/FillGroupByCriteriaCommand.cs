// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Groups;

/// <summary>
/// Fills an already resolved group with the clients that match the given criteria.
/// With Apply=false it only previews the matches; with Apply=true it persists the memberships.
/// </summary>
/// <param name="GroupId">Resolved id of the target group.</param>
/// <param name="GroupName">Resolved name of the target group (for the result message).</param>
/// <param name="Canton">Optional canton/state filter (matched against the client address state).</param>
/// <param name="ContractId">Optional id of an active contract the client must hold.</param>
/// <param name="City">Optional city filter (matched against the client address city).</param>
/// <param name="ZipPrefix">Optional zip code prefix filter (matched against the client address zip).</param>
/// <param name="QualificationId">Optional id of a qualification the client must currently hold.</param>
/// <param name="EntityType">Optional client type filter (defaults to Employee in the calling skill).</param>
/// <param name="Count">Optional maximum number of clients to add; null means all matches up to the search cap.</param>
/// <param name="ValidFrom">Start date of the new memberships (the plannability boundary); null defaults to now. Ignored when StartMode is EntryDate.</param>
/// <param name="Apply">False for a dry-run preview, true to persist the memberships.</param>
/// <param name="UserName">Name of the acting user, stored on the created memberships.</param>
/// <param name="StartMode">Fixed starts every membership on ValidFrom; EntryDate starts each person's membership on their own employment entry date.</param>
/// <param name="RandomSeed">Null selects the first matches by name; a value draws Count matches at random with this
/// seed, so the apply call that passes the preview's seed adds exactly the previewed clients.</param>
public record FillGroupByCriteriaCommand(
    Guid GroupId,
    string GroupName,
    string? Canton,
    Guid? ContractId,
    string? City,
    string? ZipPrefix,
    Guid? QualificationId,
    EntityTypeEnum? EntityType,
    int? Count,
    DateTime? ValidFrom,
    bool Apply,
    string UserName,
    GroupMembershipStartMode StartMode = GroupMembershipStartMode.Fixed,
    int? RandomSeed = null) : IRequest<FillGroupByCriteriaResult>;
