// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
/// <param name="EntityType">Optional client type filter (defaults to Employee in the calling skill).</param>
/// <param name="Count">Optional maximum number of clients to add; null means all matches up to the search cap.</param>
/// <param name="Apply">False for a dry-run preview, true to persist the memberships.</param>
/// <param name="UserName">Name of the acting user, stored on the created memberships.</param>
public record FillGroupByCriteriaCommand(
    Guid GroupId,
    string GroupName,
    string? Canton,
    Guid? ContractId,
    EntityTypeEnum? EntityType,
    int? Count,
    bool Apply,
    string UserName) : IRequest<FillGroupByCriteriaResult>;
