// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The outcome of asking for an identity to run one proactive action under: either a ready-to-use
/// <see cref="SkillExecutionContext"/> plus the rights it carries, or a refusal with its category and a
/// human-readable reason. Deliberately a result rather than an exception - every refusal here is an
/// ordinary operational state (no owner configured, owner deactivated, skill reclassified as sensitive)
/// that the caller has to log and skip, not an error that should unwind a tick.
/// </summary>
/// <param name="Success">True when <paramref name="Context"/> is present and the action may run.</param>
/// <param name="Context">Execution context carrying the minted token, the acting user and the proactive SessionId. Null on refusal.</param>
/// <param name="UserPermissions">The owner's CURRENT rights, expanded from the roles the token was minted with. Empty on refusal.</param>
/// <param name="Refusal">Which of the refusal categories applies, None on success.</param>
/// <param name="Reason">Human-readable refusal text, null on success.</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Assistant;

public sealed record ProactiveActionIdentity(
    bool Success,
    SkillExecutionContext? Context,
    IReadOnlyList<string> UserPermissions,
    ProactiveActionIdentityRefusal Refusal,
    string? Reason)
{
    public static ProactiveActionIdentity Resolved(
        SkillExecutionContext context, IReadOnlyList<string> userPermissions) =>
        new(true, context, userPermissions, ProactiveActionIdentityRefusal.None, null);

    public static ProactiveActionIdentity Refused(ProactiveActionIdentityRefusal refusal, string reason) =>
        new(false, null, Array.Empty<string>(), refusal, reason);
}
