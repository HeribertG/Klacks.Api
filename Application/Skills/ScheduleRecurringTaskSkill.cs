// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates (or updates, by name) a recurring task Klacksy runs on a cron schedule for the current user.
/// The action is resolved to a concrete artifact here, at authoring time: a static reminder text or a
/// single deterministic skill invocation — so the scheduled run needs no LLM and no further input. With
/// apply=false (default) it validates everything and returns a preview with the next run for the user to
/// confirm; with apply=true it persists the task and captures the owner's identity and permissions.
/// </summary>
/// <param name="name">Human-readable label, unique per user; re-using a name updates that task.</param>
/// <param name="cronExpression">Standard 5-field cron expression derived from the user's natural-language schedule (e.g. "0 8 * * 1" = Mondays 08:00).</param>
/// <param name="actionType">"reminder" to deliver messageText, or "skill" to run a skill.</param>
/// <param name="messageText">The reminder text (required when actionType is "reminder").</param>
/// <param name="skillName">The skill to run (required when actionType is "skill").</param>
/// <param name="skillParameters">JSON object of parameters for the skill (optional).</param>
/// <param name="timeZoneId">IANA time zone for the schedule; defaults to the app owner's address country time zone.</param>
/// <param name="maxRuns">Optional cap on the number of runs; null means unlimited.</param>
/// <param name="apply">When true the task is saved; when false (default) only a preview is returned.</param>

using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Services.Assistant.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("schedule_recurring_task")]
public class ScheduleRecurringTaskSkill : BaseSkillImplementation
{
    private static readonly HashSet<string> SchedulingSkillNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "schedule_recurring_task",
        "list_recurring_tasks",
        "cancel_recurring_task"
    };

    private readonly IScheduledTaskRepository _repository;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly ISettingsReader _settingsReader;

    public ScheduleRecurringTaskSkill(
        IScheduledTaskRepository repository,
        ISkillRegistry skillRegistry,
        ISkillRiskClassifier riskClassifier,
        ISettingsReader settingsReader)
    {
        _repository = repository;
        _skillRegistry = skillRegistry;
        _riskClassifier = riskClassifier;
        _settingsReader = settingsReader;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name").Trim();
        var cronExpression = GetRequiredString(parameters, "cronExpression").Trim();
        var actionType = GetRequiredString(parameters, "actionType").Trim().ToLowerInvariant();
        var messageText = GetParameter<string>(parameters, "messageText");
        var skillName = GetParameter<string>(parameters, "skillName")?.Trim();
        var skillParameters = GetParameter<string>(parameters, "skillParameters");
        var timeZoneId = GetParameter<string>(parameters, "timeZoneId");
        var maxRuns = GetParameter<int?>(parameters, "maxRuns");
        var apply = GetParameter<bool?>(parameters, "apply") ?? false;

        if (actionType != ScheduledTaskActionTypes.Reminder && actionType != ScheduledTaskActionTypes.Skill)
        {
            return SkillResult.Error(
                $"Unknown actionType '{actionType}'. Use '{ScheduledTaskActionTypes.Reminder}' or '{ScheduledTaskActionTypes.Skill}'.");
        }

        var resolvedTimeZone = await ResolveTimeZoneAsync(timeZoneId, context);

        if (!CronSchedule.IsValidTimeZone(resolvedTimeZone))
        {
            return SkillResult.Error(
                $"Unknown time zone '{resolvedTimeZone}'. Use an IANA id such as 'Europe/Zurich'.");
        }

        if (!CronSchedule.IsValidExpression(cronExpression))
        {
            return SkillResult.Error(
                $"Invalid cron expression '{cronExpression}'. Use standard 5-field cron, e.g. '0 8 * * 1' for Mondays at 08:00.");
        }

        var parametersJson = "{}";
        if (actionType == ScheduledTaskActionTypes.Reminder)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return SkillResult.Error("A reminder needs messageText.");
            }

            skillName = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(skillName))
            {
                return SkillResult.Error("A skill action needs skillName.");
            }

            if (SchedulingSkillNames.Contains(skillName))
            {
                return SkillResult.Error("Scheduling skills cannot themselves be scheduled.");
            }

            var descriptor = _skillRegistry.GetSkillByName(skillName);
            if (descriptor is null)
            {
                return SkillResult.Error($"Unknown skill '{skillName}'. It cannot be scheduled.");
            }

            if (_riskClassifier.Classify(descriptor) == SkillRiskClass.Sensitive)
            {
                return SkillResult.Error($"Skill '{skillName}' is too sensitive to run unattended on a schedule.");
            }

            if (!string.IsNullOrWhiteSpace(skillParameters))
            {
                if (!TryNormalizeJsonObject(skillParameters, out var normalized))
                {
                    return SkillResult.Error("skillParameters must be a JSON object, e.g. {\"key\":\"value\"}.");
                }

                parametersJson = normalized;
            }
        }

        var now = DateTime.UtcNow;
        var nextRunUtc = CronSchedule.GetNextOccurrenceUtc(cronExpression, resolvedTimeZone, now);
        if (nextRunUtc is null)
        {
            return SkillResult.Error("This schedule has no upcoming occurrence; adjust the cron expression.");
        }

        var nextRunLocal = CronSchedule.FormatLocal(nextRunUtc.Value, resolvedTimeZone);
        var effectiveMaxRuns = maxRuns is { } m && m > 0 ? m : (int?)null;

        var preview = new
        {
            name,
            cronExpression,
            timeZone = resolvedTimeZone,
            actionType,
            skillName,
            nextRun = nextRunLocal,
            maxRuns = effectiveMaxRuns
        };

        if (!apply)
        {
            return SkillResult.SuccessResult(
                preview,
                $"Preview: '{name}' would run [{cronExpression}] in {resolvedTimeZone}. " +
                $"Next run: {nextRunLocal}. Nothing was scheduled yet. " +
                "Ask the user to confirm, then call again with apply=true.");
        }

        var existing = await _repository.GetByOwnerAndNameAsync(context.UserId, name, cancellationToken);
        var ownerPermissions = string.Join(",", context.UserPermissions);

        if (existing is null)
        {
            var task = new ScheduledTask
            {
                Name = name,
                CronExpression = cronExpression,
                TimeZoneId = resolvedTimeZone,
                ActionType = actionType,
                MessageText = actionType == ScheduledTaskActionTypes.Reminder ? messageText : null,
                SkillName = skillName,
                ParametersJson = parametersJson,
                OwnerUserId = context.UserId,
                OwnerUserName = context.UserName,
                OwnerPermissionsCsv = ownerPermissions,
                IsEnabled = true,
                NextRunUtc = nextRunUtc,
                MaxRuns = effectiveMaxRuns
            };

            await _repository.AddAsync(task, cancellationToken);
        }
        else
        {
            existing.CronExpression = cronExpression;
            existing.TimeZoneId = resolvedTimeZone;
            existing.ActionType = actionType;
            existing.MessageText = actionType == ScheduledTaskActionTypes.Reminder ? messageText : null;
            existing.SkillName = skillName;
            existing.ParametersJson = parametersJson;
            existing.OwnerUserName = context.UserName;
            existing.OwnerPermissionsCsv = ownerPermissions;
            existing.IsEnabled = true;
            existing.NextRunUtc = nextRunUtc;
            existing.MaxRuns = effectiveMaxRuns;
            existing.RunCount = 0;

            await _repository.UpdateAsync(existing, cancellationToken);
        }

        return SkillResult.SuccessResult(
            preview,
            $"Scheduled '{name}' [{cronExpression}] in {resolvedTimeZone}. Next run: {nextRunLocal}.");
    }

    private async Task<string> ResolveTimeZoneAsync(string? explicitTimeZoneId, SkillExecutionContext context)
    {
        if (!string.IsNullOrWhiteSpace(explicitTimeZoneId))
        {
            return explicitTimeZoneId.Trim();
        }

        var countrySetting = await _settingsReader.GetSetting(SettingKeys.GlobalCalendarCountry);
        var ownerTimeZone = CountryTimeZones.Resolve(countrySetting?.Value);
        if (!string.IsNullOrWhiteSpace(ownerTimeZone))
        {
            return ownerTimeZone!;
        }

        if (!string.IsNullOrWhiteSpace(context.UserTimezone))
        {
            return context.UserTimezone!;
        }

        return TimeZoneDefaults.DefaultTimezone;
    }

    private static bool TryNormalizeJsonObject(string json, out string normalized)
    {
        normalized = "{}";
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            normalized = document.RootElement.GetRawText();
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
