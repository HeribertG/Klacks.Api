using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ConfigureHeartbeatSkill : BaseSkill
{
    private readonly IHeartbeatConfigRepository _repo;

    public ConfigureHeartbeatSkill(IHeartbeatConfigRepository repo)
    {
        _repo = repo;
    }

    public override string Name => "configure_heartbeat";

    public override string Description =>
        "Configure the proactive heartbeat monitoring. Use 'enable' to activate monitoring, 'disable' to stop it, " +
        "'configure' to set checklist and interval, or 'status' to view current configuration.";

    public override SkillCategory Category => SkillCategory.System;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanViewSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new("action", "The action to perform", SkillParameterType.Enum, true,
            EnumValues: new[] { "enable", "disable", "configure", "status" }),
        new("checklist", "JSON array of items to monitor, e.g. [\"shift conflicts\", \"contract expirations\"]",
            SkillParameterType.String, false),
        new("intervalMinutes", "How often to check in minutes (default: 30)",
            SkillParameterType.Integer, false, 30)
    ];

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

    private static SkillResult HandleStatus(Domain.Models.Assistant.HeartbeatConfig? config)
    {
        if (config == null)
            return SkillResult.SuccessResult(new { configured = false },
                "Heartbeat monitoring is not configured yet.");

        return SkillResult.SuccessResult(new
        {
            configured = true,
            enabled = config.IsEnabled,
            intervalMinutes = config.IntervalMinutes,
            activeHoursStart = config.ActiveHoursStart.ToString("HH:mm"),
            activeHoursEnd = config.ActiveHoursEnd.ToString("HH:mm"),
            checklist = config.ChecklistJson,
            lastExecutedAt = config.LastExecutedAt?.ToString("o"),
            onboardingCompleted = config.OnboardingCompleted
        }, config.IsEnabled
            ? $"Heartbeat is active, checking every {config.IntervalMinutes} minutes."
            : "Heartbeat is configured but currently disabled.");
    }

    private async Task<SkillResult> HandleEnableAsync(
        string userId,
        Domain.Models.Assistant.HeartbeatConfig? config,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (config == null)
        {
            config = new Domain.Models.Assistant.HeartbeatConfig
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsEnabled = true,
                OnboardingCompleted = true,
                IntervalMinutes = GetParameter<int?>(parameters, "intervalMinutes") ?? 30,
                ChecklistJson = GetParameter<string>(parameters, "checklist") ?? "[]"
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
                config.ChecklistJson = GetParameter<string>(parameters, "checklist") ?? "[]";
            await _repo.UpdateAsync(config, cancellationToken);
        }

        return SkillResult.SuccessResult(new { enabled = true },
            $"Heartbeat monitoring enabled. Checking every {config.IntervalMinutes} minutes.");
    }

    private async Task<SkillResult> HandleDisableAsync(
        Domain.Models.Assistant.HeartbeatConfig? config,
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
        Domain.Models.Assistant.HeartbeatConfig? config,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        if (config == null)
        {
            config = new Domain.Models.Assistant.HeartbeatConfig
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                IsEnabled = false,
                OnboardingCompleted = true,
                IntervalMinutes = GetParameter<int?>(parameters, "intervalMinutes") ?? 30,
                ChecklistJson = GetParameter<string>(parameters, "checklist") ?? "[]"
            };
            await _repo.AddAsync(config, cancellationToken);
        }
        else
        {
            config.OnboardingCompleted = true;
            if (parameters.ContainsKey("intervalMinutes"))
                config.IntervalMinutes = GetParameter<int>(parameters, "intervalMinutes");
            if (parameters.ContainsKey("checklist"))
                config.ChecklistJson = GetParameter<string>(parameters, "checklist") ?? "[]";
            await _repo.UpdateAsync(config, cancellationToken);
        }

        return SkillResult.SuccessResult(new
        {
            intervalMinutes = config.IntervalMinutes,
            checklist = config.ChecklistJson,
            enabled = config.IsEnabled
        }, $"Heartbeat configuration updated. Interval: {config.IntervalMinutes} min. " +
           (config.IsEnabled ? "Monitoring is active." : "Use 'enable' to start monitoring."));
    }
}
