// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 5 meta-skill — reports the most recent skill execution for the current user.
/// Reads from agent_skill_executions filtered by triggered_by = userId, ordered desc by create_time.
/// Used by the LLM to confirm "did my last mutating skill succeed?" or to inform a rollback decision.
/// </summary>

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("verify_my_last_action")]
public class VerifyMyLastActionSkill : BaseSkillImplementation
{
    private readonly DataBaseContext _context;

    public VerifyMyLastActionSkill(DataBaseContext context)
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
            .Where(e => e.TriggeredBy == userIdStr && e.CreateTime >= since)
            .OrderByDescending(e => e.CreateTime)
            .Select(e => new
            {
                e.Id,
                e.ToolName,
                e.Success,
                e.ResultMessage,
                e.ErrorMessage,
                e.DurationMs,
                e.CreateTime,
                e.ParametersJson
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (execution == null)
        {
            return SkillResult.SuccessResult(
                new { Status = "no_recent_execution" },
                "No skill execution found for the current user in the last 30 minutes.");
        }

        var status = execution.Success ? "succeeded" : "failed";
        return SkillResult.SuccessResult(
            new
            {
                Status = status,
                Skill = execution.ToolName,
                Success = execution.Success,
                Message = execution.ResultMessage ?? execution.ErrorMessage,
                DurationMs = execution.DurationMs,
                ExecutedAt = execution.CreateTime,
                ParametersJson = execution.ParametersJson
            },
            $"Last execution: {execution.ToolName} {status} {execution.DurationMs}ms ago. " +
            (execution.Success
                ? "Use rollback_my_last_change if you want to undo it."
                : $"Failure reason: {execution.ErrorMessage}"));
    }
}
