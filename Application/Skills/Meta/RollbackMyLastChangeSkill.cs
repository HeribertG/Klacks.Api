// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 5 meta-skill — proposes a rollback path for the most recent mutating skill execution. Does
/// NOT execute the rollback itself (HITL contract); instead returns the inverse-skill name + the
/// parameter mapping hint so the LLM (or user) can decide and confirm before calling it.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("rollback_my_last_change")]
public class RollbackMyLastChangeSkill : BaseSkillImplementation
{
    private readonly DataBaseContext _context;

    public RollbackMyLastChangeSkill(DataBaseContext context)
    {
        _context = context;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var userIdStr = context.UserId.ToString();
        var since = DateTime.UtcNow.AddMinutes(-30);

        var execution = await _context.Set<AgentSkillExecution>()
            .AsNoTracking()
            .Where(e => e.TriggeredBy == userIdStr && e.CreateTime >= since && e.Success)
            .OrderByDescending(e => e.CreateTime)
            .Select(e => new { e.Id, e.ToolName, e.ResultMessage, e.ParametersJson, e.CreateTime })
            .FirstOrDefaultAsync(cancellationToken);

        if (execution == null)
        {
            return SkillResult.Error("No successful skill execution found for the current user in the last 30 minutes.");
        }

        if (!InverseSkillRegistry.TryGet(execution.ToolName, out var inverse))
        {
            return SkillResult.Error(
                $"Skill '{execution.ToolName}' is not in the rollback whitelist — escalate to the human operator. " +
                "Add an entry to InverseSkillRegistry when its inverse skill exists.");
        }

        if (string.Equals(inverse.SkillName, "__manual__", StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Error(
                $"Skill '{execution.ToolName}' has no automatic rollback path: {inverse.ParamHint}. " +
                "Escalate to the human operator.");
        }

        return SkillResult.SuccessResult(
            new
            {
                ExecutionId = execution.Id,
                OriginalSkill = execution.ToolName,
                InverseSkill = inverse.SkillName,
                ParamHint = inverse.ParamHint,
                OriginalParametersJson = execution.ParametersJson,
                OriginalResultMessage = execution.ResultMessage
            },
            $"Rollback proposal: call '{inverse.SkillName}' to undo '{execution.ToolName}'. " +
            $"Param hint: {inverse.ParamHint}. " +
            "This skill DOES NOT execute the rollback — confirm with the user first, then call the inverse skill explicitly.");
    }
}
