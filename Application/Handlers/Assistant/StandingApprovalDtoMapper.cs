// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps stored standing approvals onto their transport shape. Hand-written rather than generated because
/// of the one computed field: IsActive is not a column but the answer of
/// <see cref="StandingApprovalPolicy"/> at a given instant, and it is computed here from the caller's
/// "now" so the admin card can never disagree with the action dispatcher about whether a grant still
/// applies. The list form compiles that predicate once for the whole page rather than once per row.
/// </summary>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Handlers.Assistant;

public static class StandingApprovalDtoMapper
{
    public static StandingApprovalDto ToDto(StandingApproval approval, DateTime nowUtc) =>
        ToDto(approval, StandingApprovalPolicy.ActiveAt(nowUtc).Compile());

    public static IReadOnlyList<StandingApprovalDto> ToDtos(
        IReadOnlyList<StandingApproval> approvals, DateTime nowUtc)
    {
        var isActive = StandingApprovalPolicy.ActiveAt(nowUtc).Compile();
        return approvals.Select(approval => ToDto(approval, isActive)).ToList();
    }

    private static StandingApprovalDto ToDto(
        StandingApproval approval, Func<StandingApproval, bool> isActive) =>
        new(
            approval.Id,
            approval.TriggerKind,
            approval.GroupId,
            approval.GrantedByUserId,
            approval.GrantedAtUtc,
            approval.ExpiresAtUtc,
            approval.DailyBudget,
            approval.RevokedAtUtc,
            approval.RevokedByUserId,
            isActive(approval));
}
