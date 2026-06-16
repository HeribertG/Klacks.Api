// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Services.Assistant.SkillGraph;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

/// <summary>
/// Decides whether a successful skill execution should trigger a client-side page reload and,
/// if so, pushes an entity-changed notification to the acting user. Read-only and scenario-gated
/// skills never notify (no direct DB write); schedule-domain entities are excluded because the
/// work-notifications hub already pushes those live.
/// </summary>
public class EntityChangeNotifier : IEntityChangeNotifier
{
    private static readonly HashSet<string> ScheduleEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "work", "break", "expenses", "workchange", "schedulecommand", "analysescenario", "clientperiodhours"
    };

    private static readonly string[] CreatePrefixes = { "create_", "add_" };
    private static readonly string[] DeletePrefixes = { "delete_", "remove_" };
    private static readonly string[] UpdatePrefixes = { "update_", "set_", "assign_" };

    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly IAssistantNotificationService _notificationService;
    private readonly ILogger<EntityChangeNotifier> _logger;

    public EntityChangeNotifier(
        ISkillRiskClassifier riskClassifier,
        IAssistantNotificationService notificationService,
        ILogger<EntityChangeNotifier> logger)
    {
        _riskClassifier = riskClassifier;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task NotifyExecutedAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        SkillResult result,
        CancellationToken cancellationToken = default)
    {
        if (!result.Success || context.UserId == Guid.Empty)
        {
            return;
        }

        var riskClass = _riskClassifier.Classify(descriptor);
        if (riskClass == SkillRiskClass.ReadOnly || riskClass == SkillRiskClass.ScenarioGated)
        {
            return;
        }

        var entities = ResolveEntities(descriptor.Name);
        if (entities.Count == 0 || ScheduleEntities.Contains(entities[0]))
        {
            return;
        }

        var operation = ResolveOperation(descriptor.Name);

        await _notificationService.SendEntityChangedAsync(
            context.UserId.ToString(),
            entities,
            operation,
            descriptor.Name);

        _logger.LogDebug(
            "Entity-changed notification queued for user {UserId}: skill {SkillName} ({Operation}) on {Entities}",
            context.UserId, descriptor.Name, operation, string.Join(",", entities));
    }

    private static IReadOnlyList<string> ResolveEntities(string skillName)
    {
        if (SkillEntityMap.Map.TryGetValue(skillName, out var mapped) && mapped.Length > 0)
        {
            return mapped.Select(e => e.ToLowerInvariant()).ToList();
        }

        var derived = DeriveEntityFromName(skillName);
        return derived == null ? Array.Empty<string>() : new[] { derived };
    }

    private static string? DeriveEntityFromName(string skillName)
    {
        foreach (var prefix in CreatePrefixes.Concat(DeletePrefixes).Concat(UpdatePrefixes))
        {
            if (skillName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var entity = skillName[prefix.Length..].Replace("_", string.Empty).ToLowerInvariant();
                return string.IsNullOrEmpty(entity) ? null : entity;
            }
        }

        return null;
    }

    private static string ResolveOperation(string skillName)
    {
        if (DeletePrefixes.Any(p => skillName.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return "delete";
        }

        if (CreatePrefixes.Any(p => skillName.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            return "create";
        }

        return "update";
    }
}
