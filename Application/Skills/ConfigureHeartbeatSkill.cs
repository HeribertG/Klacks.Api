// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("configure_heartbeat")]
public class ConfigureHeartbeatSkill : BaseSkillImplementation
{
    private readonly IHeartbeatConfigRepository _repo;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ConfigureHeartbeatSkill(IHeartbeatConfigRepository repo)
    {
        _repo = repo;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var action = GetRequiredString(parameters, "action").ToLowerInvariant();
        var userId = context.UserId.ToString();

        var config = await _repo.GetByUserIdAsync(userId, cancellationToken);

        return action switch
        {
            "status" => HandleStatus(config),
            "enable" => await HandleEnableAsync(userId, config, parameters, cancellationToken),
            "disable" => await HandleDisableAsync(config, cancellationToken),
            "configure" => await HandleConfigureAsync(userId, config, parameters, cancellationToken),
            _ => SkillResult.Error($"Unknown action: {action}. Valid actions: enable, disable, configure, status")
        };
    }

    private static SkillResult HandleStatus(HeartbeatConfig? config)
    {
        if (config == null)
            return SkillResult.SuccessResult(new { configured = false },
                "Heartbeat monitoring is not configured yet.");

        var checkItems = ParseChecklistItems(config.ChecklistJson);

        return SkillResult.SuccessResult(new
        {
            configured = true,
            enabled = config.IsEnabled,
            intervalMinutes = config.IntervalMinutes,
            activeHoursStart = config.ActiveHoursStart.ToString("HH:mm"),
            activeHoursEnd = config.ActiveHoursEnd.ToString("HH:mm"),
            checklist = checkItems,
            lastExecutedAt = config.LastExecutedAt?.ToString("o"),
            onboardingCompleted = config.OnboardingCompleted
        }, config.IsEnabled
            ? $"Heartbeat is active, checking every {config.IntervalMinutes} minutes. " +
              $"{checkItems.Count(i => i.IsEnabled)} active check items."
            : "Heartbeat is configured but currently disabled.");
    }

    private async Task<SkillResult> HandleEnableAsync(
        string userId,
        HeartbeatConfig? config,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (config == null)
        {
            config = new HeartbeatConfig
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsEnabled = true,
                OnboardingCompleted = true,
                IntervalMinutes = GetParameter<int?>(parameters, "intervalMinutes") ?? HeartbeatDefaults.DefaultIntervalMinutes,
                ActiveHoursStart = HeartbeatDefaults.DefaultActiveHoursStart,
                ActiveHoursEnd = HeartbeatDefaults.DefaultActiveHoursEnd,
                ChecklistJson = GetParameter<string>(parameters, "checklist") ?? HeartbeatDefaults.DefaultChecklistJson
            };
            await _repo.AddAsync(config, cancellationToken);
        }
        else
        {
            config.IsEnabled = true;
            config.OnboardingCompleted = true;
            if (parameters.ContainsKey("intervalMinutes"))
                config.IntervalMinutes = GetParameter<int>(parameters, "intervalMinutes");
            if (parameters.ContainsKey("checklist"))
                config.ChecklistJson = GetParameter<string>(parameters, "checklist") ?? HeartbeatDefaults.DefaultChecklistJson;
            await _repo.UpdateAsync(config, cancellationToken);
        }

        return SkillResult.SuccessResult(new { enabled = true },
            $"Heartbeat monitoring enabled. Checking every {config.IntervalMinutes} minutes.");
    }

    private async Task<SkillResult> HandleDisableAsync(
        HeartbeatConfig? config,
        CancellationToken cancellationToken)
    {
        if (config == null)
            return SkillResult.Error("Heartbeat monitoring is not configured yet.");

        config.IsEnabled = false;
        await _repo.UpdateAsync(config, cancellationToken);

        return SkillResult.SuccessResult(new { enabled = false },
            "Heartbeat monitoring has been disabled.");
    }

    private async Task<SkillResult> HandleConfigureAsync(
        string userId,
        HeartbeatConfig? config,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (config == null)
        {
            config = new HeartbeatConfig
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsEnabled = false,
                OnboardingCompleted = true,
                IntervalMinutes = GetParameter<int?>(parameters, "intervalMinutes") ?? HeartbeatDefaults.DefaultIntervalMinutes,
                ActiveHoursStart = ParseTimeOnly(GetParameter<string>(parameters, "activeHoursStart"))
                    ?? HeartbeatDefaults.DefaultActiveHoursStart,
                ActiveHoursEnd = ParseTimeOnly(GetParameter<string>(parameters, "activeHoursEnd"))
                    ?? HeartbeatDefaults.DefaultActiveHoursEnd,
                ChecklistJson = GetParameter<string>(parameters, "checklist") ?? HeartbeatDefaults.DefaultChecklistJson
            };
            await _repo.AddAsync(config, cancellationToken);
        }
        else
        {
            config.OnboardingCompleted = true;
            if (parameters.ContainsKey("intervalMinutes"))
                config.IntervalMinutes = GetParameter<int>(parameters, "intervalMinutes");
            if (parameters.ContainsKey("checklist"))
                config.ChecklistJson = GetParameter<string>(parameters, "checklist") ?? HeartbeatDefaults.DefaultChecklistJson;
            if (parameters.ContainsKey("activeHoursStart"))
            {
                var parsed = ParseTimeOnly(GetParameter<string>(parameters, "activeHoursStart"));
                if (parsed.HasValue) config.ActiveHoursStart = parsed.Value;
            }
            if (parameters.ContainsKey("activeHoursEnd"))
            {
                var parsed = ParseTimeOnly(GetParameter<string>(parameters, "activeHoursEnd"));
                if (parsed.HasValue) config.ActiveHoursEnd = parsed.Value;
            }
            await _repo.UpdateAsync(config, cancellationToken);
        }

        return SkillResult.SuccessResult(new
        {
            intervalMinutes = config.IntervalMinutes,
            activeHoursStart = config.ActiveHoursStart.ToString("HH:mm"),
            activeHoursEnd = config.ActiveHoursEnd.ToString("HH:mm"),
            checklist = ParseChecklistItems(config.ChecklistJson),
            enabled = config.IsEnabled
        }, $"Heartbeat configuration updated. Interval: {config.IntervalMinutes} min. " +
           (config.IsEnabled ? "Monitoring is active." : "Use 'enable' to start monitoring."));
    }

    private static List<HeartbeatCheckItem> ParseChecklistItems(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<HeartbeatCheckItem>>(json, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static TimeOnly? ParseTimeOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return TimeOnly.TryParseExact(value, "HH:mm", out var result) ? result : null;
    }
}
