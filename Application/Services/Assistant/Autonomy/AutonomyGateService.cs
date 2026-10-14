// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Decides per invocation whether the user's autonomy level allows direct execution.
/// Read-only skills always pass; sensitive skills always require confirmation; reversible
/// and scenario-gated skills need level Assisted or higher; irreversible skills need level
/// Autonomous or higher. A valid one-time confirmation token bypasses the gate exactly once
/// and only for the exact parameters it was issued for — on any mismatch the token is burned
/// and a fresh confirmation is required. A token is never redeemable in the turn that issued it and, in a later
/// turn, only when the call carries user-confirmation evidence (IConfirmationRedemptionGuard, checked BEFORE the
/// token is consumed); such a refusal returns a clean skill error and leaves the token valid. When a sensitive skill is held and a registered
/// <see cref="ISkillConfirmationPreviewProvider"/> supports it, the server first computes a preview of exactly this call:
/// the preview is appended to the confirmation request, while a refusal or a failing provider returns an error without
/// issuing a token. The preview is facts for the model to relay; the user still sees the model's wording. The facts a
/// preview binds (SkillConfirmationPreview.Binding) are stored with the token; the confirmed replay hands them to the
/// skill, which refuses when its plan no longer matches. A skill with a preview provider is therefore redeemed only
/// through confirm_pending_action: a token sent along with the call itself is refused before it is consumed (the token
/// stays valid), because that path gives the skill no binding to compare.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Autonomy;

public class AutonomyGateService : IAutonomyGate
{
    private const string PreviewIntro =
        "\nServer-computed preview of exactly this action (show these facts to the user before asking for the "
        + "confirmation; keep every name and number unchanged, translate only the wording):\n";
    private const string PreviewFailedMessage =
        "The server could not compute the preview of '{0}', so the action was not stored and nothing was changed. "
        + "Tell the user that the preview failed; do not retry in this turn.";

    private readonly IAgentAutonomyPreferenceRepository _preferenceRepository;
    private readonly ISkillRiskClassifier _riskClassifier;
    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ITurnConfirmationScope _turnScope;
    private readonly IConfirmationRedemptionGuard _redemptionGuard;
    private readonly IReadOnlyList<ISkillConfirmationPreviewProvider> _previewProviders;
    private readonly ILogger<AutonomyGateService> _logger;

    public AutonomyGateService(
        IAgentAutonomyPreferenceRepository preferenceRepository,
        ISkillRiskClassifier riskClassifier,
        IPendingConfirmationStore confirmationStore,
        ITurnConfirmationScope turnScope,
        IConfirmationRedemptionGuard redemptionGuard,
        IEnumerable<ISkillConfirmationPreviewProvider> previewProviders,
        ILogger<AutonomyGateService> logger)
    {
        _preferenceRepository = preferenceRepository;
        _riskClassifier = riskClassifier;
        _confirmationStore = confirmationStore;
        _turnScope = turnScope;
        _redemptionGuard = redemptionGuard;
        _previewProviders = previewProviders.ToList();
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

        var redemption = TryConsumeToken(descriptor, context, parameters);
        if (redemption.Redeemed)
        {
            return null;
        }

        var level = await GetLevelAsync(context.UserId, cancellationToken);
        if (IsAllowed(riskClass, level))
        {
            return null;
        }

        if (redemption.RefusalMessage != null)
        {
            return SkillResult.Error(redemption.RefusalMessage);
        }

        var preview = riskClass == SkillRiskClass.Sensitive
            ? await BuildPreviewAsync(descriptor.Name, context, parameters, cancellationToken)
            : null;
        if (preview is { IsRefusal: true })
        {
            _logger.LogInformation(
                "Autonomy gate refused skill {SkillName} for user {UserId} before confirmation: its preview rejected the call",
                descriptor.Name, context.UserId);
            return SkillResult.Error(preview.Text);
        }

        var token = _confirmationStore.Create(
            context.UserId,
            descriptor.Name,
            parameters,
            PendingConfirmationPurposes.GateReplay,
            context.ConfirmationChannel,
            preview?.Binding);
        _turnScope.MarkIssued(token);

        _logger.LogInformation(
            "Autonomy gate held skill {SkillName} (risk {RiskClass}) for user {UserId} at level {Level}",
            descriptor.Name, riskClass, context.UserId, level);

        return SkillResult.Confirmation(
            AppendPreview(
                BuildConfirmationMessage(descriptor.Name, riskClass, level, token, context.ConfirmationChannel), preview),
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

    private TokenRedemption TryConsumeToken(
        SkillDescriptor descriptor,
        SkillExecutionContext context,
        Dictionary<string, object> parameters)
    {
        if (!parameters.TryGetValue(AutonomyDefaults.ConfirmationTokenParameter, out var rawToken))
        {
            return TokenRedemption.NotRedeemed;
        }

        parameters.Remove(AutonomyDefaults.ConfirmationTokenParameter);

        var token = rawToken?.ToString();
        if (string.IsNullOrWhiteSpace(token))
        {
            return TokenRedemption.NotRedeemed;
        }

        var verdict = _redemptionGuard.Evaluate(token, context);
        if (verdict != ConfirmationRedemptionVerdict.Allowed)
        {
            _logger.LogWarning(
                "Confirmation token for skill {SkillName} and user {UserId} refused ({Verdict}) — the user must " +
                "answer with an explicit yes in a later turn first; the token stays valid",
                descriptor.Name, context.UserId, verdict);
            return TokenRedemption.Refused(ConfirmationRedemptionMessages.For(verdict));
        }

        if (_previewProviders.Any(provider => provider.Supports(descriptor.Name)))
        {
            _logger.LogWarning(
                "Confirmation token for skill {SkillName} and user {UserId} refused: the skill shows a preview, so " +
                "its token can only be redeemed through {ConfirmSkill}; the token stays valid",
                descriptor.Name, context.UserId, AutonomyDefaults.ConfirmPendingActionSkillName);
            return TokenRedemption.Refused(
                string.Format(CultureInfo.InvariantCulture, ConfirmationBindingMessages.DirectRedemptionRefused, descriptor.Name));
        }

        var consumed = _confirmationStore.Consume(token, context.UserId, descriptor.Name, context.ConfirmationChannel);
        if (consumed == null)
        {
            _logger.LogWarning(
                "Invalid or expired confirmation token for skill {SkillName} and user {UserId}",
                descriptor.Name, context.UserId);
            return TokenRedemption.NotRedeemed;
        }

        if (!ParametersMatchStored(consumed.Parameters, parameters))
        {
            _logger.LogWarning(
                "Confirmation token for skill {SkillName} and user {UserId} was issued for different " +
                "parameters — token invalidated, a fresh confirmation is required",
                descriptor.Name, context.UserId);
            return TokenRedemption.NotRedeemed;
        }

        return TokenRedemption.Consumed;
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

    private async Task<SkillConfirmationPreview?> BuildPreviewAsync(
        string skillName,
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken)
    {
        var provider = _previewProviders.FirstOrDefault(candidate => candidate.Supports(skillName));
        if (provider == null)
        {
            return null;
        }

        try
        {
            return await provider.BuildAsync(skillName, context, parameters, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Confirmation preview for skill {SkillName} failed; the call was not held", skillName);
            return SkillConfirmationPreview.Refuse(
                string.Format(CultureInfo.InvariantCulture, PreviewFailedMessage, skillName));
        }
    }

    private static string AppendPreview(string message, SkillConfirmationPreview? preview) =>
        preview == null ? message : message + PreviewIntro + preview.Text;

    private static string BuildConfirmationMessage(
        string skillName, SkillRiskClass riskClass, AutonomyLevel level, string token, string channel)
    {
        var reason = riskClass == SkillRiskClass.Sensitive
            ? $"is classified as {riskClass} and always needs a dedicated confirmation at every autonomy level, " +
              "no matter how explicit the request sounded"
            : $"is classified as {riskClass} and the current autonomy level is {level}";

        if (string.Equals(channel, PendingConfirmationChannels.Mcp, StringComparison.Ordinal))
        {
            return $"User confirmation required: '{skillName}' {reason}. {ConfirmationRedemptionMessages.HeldOverMcp}";
        }

        return $"User confirmation required: '{skillName}' {reason}. The requested action is stored and NOT " +
               "executed yet. Ask the user to explicitly confirm this action now and stop; do not call any further " +
               "tool for it in this turn. Only in your NEXT turn, after the user answered with a clear yes in their " +
               $"own words, call '{AutonomyDefaults.ConfirmPendingActionSkillName}' with " +
               $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to '{token}'. The server refuses to redeem the " +
               "token in this same turn and in any turn in which the user's message is not an explicit yes, even " +
               "when the user's request sounded like consent. Never confirm on your own.";
    }
}
