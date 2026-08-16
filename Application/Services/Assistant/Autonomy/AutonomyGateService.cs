// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides per invocation whether the user's autonomy level allows direct execution.
/// Read-only skills always pass; sensitive skills always require confirmation; reversible
/// and scenario-gated skills need level Assisted or higher; irreversible skills need level
/// Autonomous or higher. A valid one-time confirmation token bypasses the gate exactly once
/// and only for the exact parameters it was issued for — on any mismatch the token is burned
/// and a fresh confirmation is required.
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
    private readonly ITurnConfirmationScope _turnScope;
    private readonly ILogger<AutonomyGateService> _logger;

    public AutonomyGateService(
        IAgentAutonomyPreferenceRepository preferenceRepository,
        ISkillRiskClassifier riskClassifier,
        IPendingConfirmationStore confirmationStore,
        ITurnConfirmationScope turnScope,
        ILogger<AutonomyGateService> logger)
    {
        _preferenceRepository = preferenceRepository;
        _riskClassifier = riskClassifier;
        _confirmationStore = confirmationStore;
        _turnScope = turnScope;
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
        if (riskClass == SkillRiskClass.Sensitive)
        {
            _turnScope.MarkIssuedForSensitiveSkill(token);
        }

        _logger.LogInformation(
            "Autonomy gate held skill {SkillName} (risk {RiskClass}) for user {UserId} at level {Level}",
            descriptor.Name, riskClass, context.UserId, level);

        return SkillResult.Confirmation(
            BuildConfirmationMessage(descriptor.Name, riskClass, level, token),
            token,
            new { skillName = descriptor.Name, riskClass = riskClass.ToString(), autonomyLevel = (int)level });
    }

    // Internal (not private): reused read-only by EvaluateAutonomyLevelChangeQueryHandler to compute
    // the effect of a proposed level change without duplicating this decision table.
    internal static bool IsAllowed(SkillRiskClass riskClass, AutonomyLevel level)
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

        if (_turnScope.WasIssuedThisTurnForSensitiveSkill(token))
        {
            _logger.LogWarning(
                "Confirmation token for sensitive skill {SkillName} and user {UserId} was redeemed in the " +
                "same turn it was issued — refused, the user must reply first",
                descriptor.Name, context.UserId);
            return false;
        }

        var consumed = _confirmationStore.Consume(token, context.UserId, descriptor.Name);
        if (consumed == null)
        {
            _logger.LogWarning(
                "Invalid or expired confirmation token for skill {SkillName} and user {UserId}",
                descriptor.Name, context.UserId);
            return false;
        }

        if (!ParametersMatchStored(consumed.Parameters, parameters))
        {
            _logger.LogWarning(
                "Confirmation token for skill {SkillName} and user {UserId} was issued for different " +
                "parameters — token invalidated, a fresh confirmation is required",
                descriptor.Name, context.UserId);
            return false;
        }

        return true;
    }

    private static bool ParametersMatchStored(
        IReadOnlyDictionary<string, object> stored,
        IReadOnlyDictionary<string, object> current)
    {
        var storedComparable = ToComparable(stored);
        var currentComparable = ToComparable(current);

        return storedComparable.Count == currentComparable.Count
               && storedComparable.All(kv =>
                   currentComparable.TryGetValue(kv.Key, out var value) &&
                   string.Equals(value, kv.Value, StringComparison.Ordinal));
    }

    private static Dictionary<string, string> ToComparable(IEnumerable<KeyValuePair<string, object>> parameters)
    {
        return parameters.ToDictionary(
            kv => kv.Key,
            kv => kv.Value?.ToString() ?? string.Empty,
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<AutonomyLevel> GetLevelAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _preferenceRepository.GetAsync(userId.ToString(), cancellationToken);
        return row?.Level ?? AutonomyDefaults.DefaultLevel;
    }

    private static string BuildConfirmationMessage(string skillName, SkillRiskClass riskClass, AutonomyLevel level, string token)
    {
        if (riskClass == SkillRiskClass.Sensitive)
        {
            return $"User confirmation required: '{skillName}' is classified as {riskClass} and always needs a " +
                   $"dedicated confirmation at every autonomy level, no matter how explicit the request " +
                   $"sounded. The requested action is " +
                   $"stored and NOT executed yet. Ask the user to explicitly confirm this action now and stop; " +
                   $"only in your NEXT turn, after the user confirmed in their own words, call " +
                   $"'{AutonomyDefaults.ConfirmPendingActionSkillName}' with " +
                   $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to '{token}'. Redeeming the token in this " +
                   $"same turn is refused by the server. Never confirm on your own.";
        }

        return $"User confirmation required: '{skillName}' is classified as {riskClass} and the current " +
               $"autonomy level is {level}. The requested action is stored and NOT executed yet. " +
               $"If the user already gave their explicit confirmation in their most recent message, do NOT ask " +
               $"again — immediately call '{AutonomyDefaults.ConfirmPendingActionSkillName}' with " +
               $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to '{token}'. Otherwise ask the user to " +
               $"explicitly confirm this action first, and only call '{AutonomyDefaults.ConfirmPendingActionSkillName}' " +
               $"with that token after they confirmed in their own words. Never confirm on your own.";
    }
}
