// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Grants an advance approval for one trigger kind in one scope. The granting user is the identity every
/// execution under it will borrow, so it is taken from the authenticated principal by the controller and
/// never from the request body.
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record GrantStandingApprovalCommand(
    string TriggerKind,
    Guid? GroupId,
    int? DurationDays,
    int? DailyBudget,
    Guid GrantedByUserId) : IRequest<GrantStandingApprovalResult>;
