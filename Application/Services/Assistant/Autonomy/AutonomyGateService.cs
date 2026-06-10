// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides per invocation whether the user's autonomy level allows direct execution.
/// Read-only skills always pass; sensitive skills always require confirmation; reversible
/// and scenario-gated skills need level Assisted or higher; irreversible skills need level
/// Autonomous or higher. A valid one-time confirmation token bypasses the gate exactly once.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class AutonomyGateService : IAutonomyGate
{
    private readonly IAgentAutonomyPreferenceRepository _preferenceRepository;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ILogger<AutonomyGateService> _logger;

    public AutonomyGateService(
        IAgentAutonomyPreferenceRepository preferenceRepository,
        ISkillRiskClassifier riskClassifier,
        IPendingConfirmationStore confirmationStore,
        ILogger<AutonomyGateService> logger)
    {
        _preferenceRepository = preferenceRepository;
        _riskClassifier = riskClassifier;
        _confirmationStore = confirmationStore;
        _logger = logger;
    }

    public async Task<SkillResult?> CheckAsync(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        if (context.BypassAutonomyGate)
        {
            return null;
        }

        if (string.Equals(descriptor.Name, AutonomyDefaults.ConfirmPendingActionSkillName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var riskClass = _riskClassifier.Classify(descriptor);
        if (riskClass == SkillRiskClass.ReadOnly)
        {
            return null;
        }

        if (TryConsumeToken(descriptor, context, parameters))
        {
            return null;
        }

        var level = await GetLevelAsync(context.UserId, cancellationToken);
        if (IsAllowed(riskClass, level))
        {
            return null;
        }

        var token = _confirmationStore.Create(context.UserId, descriptor.Name, parameters);
        _logger.LogInformation(
            "Autonomy gate held skill {SkillName} (risk {RiskClass}) for user {UserId} at level {Level}",
            descriptor.Name, riskClass, context.UserId, level);

        return SkillResult.Confirmation(
            BuildConfirmationMessage(descriptor.Name, riskClass, level, token),
            token,
            new { skillName = descriptor.Name, riskClass = riskClass.ToString(), autonomyLevel = (int)level });
    }

    private static bool IsAllowed(SkillRiskClass riskClass, AutonomyLevel level)
    {
        return riskClass switch
        {
            SkillRiskClass.ReadOnly => true,
            SkillRiskClass.Reversible => level >= AutonomyLevel.Assisted,
            SkillRiskClass.ScenarioGated => level >= AutonomyLevel.Assisted,
            SkillRiskClass.Irreversible => level >= AutonomyLevel.Autonomous,
            SkillRiskClass.Sensitive => false,
            _ => false
        };
    }

    private bool TryConsumeToken(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue(AutonomyDefaults.ConfirmationTokenParameter, out var rawToken))
        {
            return false;
        }

        parameters.Remove(AutonomyDefaults.ConfirmationTokenParameter);

        var token = rawToken?.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var consumed = _confirmationStore.Consume(token, context.UserId, descriptor.Name);
        if (consumed == null)
        {
            _logger.LogWarning(
                "Invalid or expired confirmation token for skill {SkillName} and user {UserId}",
                descriptor.Name, context.UserId);
        }

        return consumed != null;
    }

    private async Task<AutonomyLevel> GetLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _preferenceRepository.GetAsync(userId.ToString(), cancellationToken);
        return row?.Level ?? AutonomyDefaults.DefaultLevel;
    }

    private static string BuildConfirmationMessage(string skillName, SkillRiskClass riskClass, AutonomyLevel level, string token)
    {
        return $"User confirmation required: '{skillName}' is classified as {riskClass} and the current " +
               $"autonomy level is {level}. The requested action is stored and NOT executed yet. " +
               $"If the user already gave their explicit confirmation in their most recent message, do NOT ask " +
               $"again — immediately call '{AutonomyDefaults.ConfirmPendingActionSkillName}' with " +
               $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to '{token}'. Otherwise ask the user to " +
               $"explicitly confirm this action first, and only call '{AutonomyDefaults.ConfirmPendingActionSkillName}' " +
               $"with that token after they confirmed in their own words. Never confirm on your own.";
    }
}
