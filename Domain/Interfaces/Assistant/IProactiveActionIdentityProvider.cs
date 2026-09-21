// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mints the identity a proactive action runs under, so no caller re-assembles that sequence. It is the
/// same chain ScheduledTaskRunner walks for the cron path - fresh token for the acting user, roles
/// expanded into rights, the remediation's own permissions checked, unattended-skill policy consulted -
/// stopping deliberately one step short of ISkillExecutor.ExecuteAsync: what surrounds the execution
/// (budget, circuit breaker, quiet window, mandatory report) belongs to the action dispatcher, and
/// wrapping the executor here would fix that shape before it exists.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveActionIdentityProvider
{
    /// <summary>
    /// Resolves the identity for one action on one condition. Rights are the acting user's CURRENT roles
    /// read at mint time, never a set frozen when the approval was given or the governance rule written,
    /// so revoking a role takes effect on the next tick.
    /// </summary>
    /// <param name="approverUserId">
    /// The human whose rights the action borrows: the approver who acknowledged the condition's approval
    /// chain or delegated the condition. The stamp carries no foreign key to the user, so it may point at
    /// an account that no longer exists; that comes back as a refusal - nothing is repaired here, because
    /// deciding that an account is gone for good is a human's call.
    /// </param>
    /// <param name="conditionId">Ledger row being remedied; it names the action's SessionId.</param>
    /// <param name="skillName">Skill the action intends to run, checked against its RequiredPermissions and IUnattendedSkillPolicy.</param>
    Task<ProactiveActionIdentity> ResolveForSkillAsync(
        Guid approverUserId,
        Guid conditionId,
        string skillName,
        CancellationToken cancellationToken = default);
}
