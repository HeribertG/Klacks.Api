// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 5 skeleton: meta-skill that lets Klacksy undo its previous mutating action when it failed verification.
/// Today: returns a "not yet implemented" message.
/// TODO (next iteration): whitelist of rollback-able skills (delete created entity, restore soft-deleted
/// entity, revert lock_level, ...); skills outside the whitelist must escalate to HITL.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("rollback_my_last_change")]
public class RollbackMyLastChangeSkill : BaseSkillImplementation
{
    public override Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(SkillResult.Error(
            "rollback_my_last_change is a Phase 5 skeleton — the rollback whitelist is not yet wired up. " +
            "Escalating to the human operator for manual undo."));
    }
}
