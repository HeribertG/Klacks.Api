// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Answer of a grant attempt. A refusal carries its reason as prose because every refusal names a
/// concrete remedy (revoke the running grant, pick a kind that has a reversible remediation, stay within
/// the duration or budget limit) and an administrator has to read which one applies.
/// </summary>
/// <param name="Outcome">Whether the grant was stored, collided with a running one, or was refused.</param>
/// <param name="Reason">Null when granted; the refusal or collision explanation otherwise.</param>
/// <param name="Approval">The stored grant, only on <see cref="GrantStandingApprovalOutcome.Granted"/>.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Assistant;

public record GrantStandingApprovalResult(
    GrantStandingApprovalOutcome Outcome,
    string? Reason,
    StandingApprovalDto? Approval)
{
    public static GrantStandingApprovalResult Granted(StandingApprovalDto approval) =>
        new(GrantStandingApprovalOutcome.Granted, null, approval);

    public static GrantStandingApprovalResult AlreadyActive(string reason) =>
        new(GrantStandingApprovalOutcome.AlreadyActive, reason, null);

    public static GrantStandingApprovalResult Refused(string reason) =>
        new(GrantStandingApprovalOutcome.Refused, reason, null);
}
