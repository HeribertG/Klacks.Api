// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Mints the identity a proactive action runs under, so no caller re-assembles that sequence. It is the
/// same chain ScheduledTaskRunner walks for the cron path - fresh token for the responsible owner, roles
/// expanded into rights, unattended-skill policy consulted - stopping deliberately one step short of
/// ISkillExecutor.ExecuteAsync: what surrounds the execution (budget, circuit breaker, quiet window,
/// mandatory report) belongs to the action dispatcher, and wrapping the executor here would fix that
/// shape before it exists.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IProactiveActionIdentityProvider
{
    /// <summary>
    /// Resolves the identity for one action on one condition. Rights are the owner's CURRENT roles read
    /// at mint time, never a set frozen when the governance rule was written, so revoking a role takes
    /// effect on the next tick.
    /// </summary>
    /// <param name="responsibleOwnerUserId">
    /// The owner from the kind's governance rule. Nullable on purpose: AgentTriggerGovernance carries no
    /// foreign key to the user, so this may be absent or point at an account that no longer exists. Both
    /// come back as a refusal - the governance row is never repaired here, because deciding that an
    /// owner is gone for good is a human's call.
    /// </param>
    /// <param name="conditionId">Ledger row being remedied; it names the action's SessionId.</param>
    /// <param name="skillName">Skill the action intends to run, checked against IUnattendedSkillPolicy.</param>
    Task<ProactiveActionIdentity> ResolveForSkillAsync(
        Guid? responsibleOwnerUserId,
        Guid conditionId,
        string skillName,
        CancellationToken cancellationToken = default);
}
