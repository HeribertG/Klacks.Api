// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// What a caller authored for a recurring task, applied the same way to a new and to a re-authored task.
/// Besides the schedule and the action it stamps the owner's permission snapshot and the MCP access mode of
/// the authoring caller (null from the chat or REST); the runner turns a non-null mode into the Authorised ceiling.
/// </summary>
/// <param name="CronExpression">Validated 5-field cron expression</param>
/// <param name="TimeZoneId">Normalised IANA time zone the cron expression runs in</param>
/// <param name="ActionType">Reminder or skill, see ScheduledTaskActionTypes</param>
/// <param name="MessageText">Reminder text; dropped for a skill action</param>
/// <param name="SkillName">Skill to run for a skill action</param>
/// <param name="ParametersJson">Normalised JSON object of skill parameters</param>
/// <param name="NextRunUtc">First occurrence after authoring</param>
/// <param name="MaxRuns">Optional cap on total runs</param>
/// <param name="AllowIrreversibleUnattended">Per-task opt-in for irreversible skills</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Scheduling;

public sealed record ScheduledTaskAuthoring(
    string CronExpression,
    string TimeZoneId,
    string ActionType,
    string? MessageText,
    string? SkillName,
    string ParametersJson,
    DateTime? NextRunUtc,
    int? MaxRuns,
    bool AllowIrreversibleUnattended)
{
    public void ApplyTo(ScheduledTask task, SkillExecutionContext context)
    {
        task.CronExpression = CronExpression;
        task.TimeZoneId = TimeZoneId;
        task.ActionType = ActionType;
        task.MessageText = ActionType == ScheduledTaskActionTypes.Reminder ? MessageText : null;
        task.SkillName = SkillName;
        task.ParametersJson = ParametersJson;
        task.OwnerUserName = context.UserName;
        task.OwnerPermissionsCsv = string.Join(",", context.UserPermissions);
        task.ExternalAgentAccessMode = context.ExternalAgentAccessMode;
        task.IsEnabled = true;
        task.NextRunUtc = NextRunUtc;
        task.MaxRuns = MaxRuns;
        task.AllowIrreversibleUnattended = AllowIrreversibleUnattended;
    }
}
