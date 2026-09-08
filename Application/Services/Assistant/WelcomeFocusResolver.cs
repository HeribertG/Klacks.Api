// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the single most urgent open state of the installation for the welcome toast. Reads the
/// condition journal instead of running the detectors - two of them are N+1 over every group and
/// NextPeriodSchedulingDueDetector starts the auto wizard as a side effect - and adds one fresh
/// setup probe on top, so a brand new installation is answered correctly before the hourly
/// heartbeat has ever run. Journal rows of kind no_schedule_yet are always discarded: either the
/// fresh probe already produced the current stage, or work exists and the row is stale.
/// </summary>
/// <param name="scopeResolver">Decides whether this user is a planner and which group roots they see</param>
/// <param name="setupProbe">Fresh installation-wide snapshot along orders -> shifts -> assignments</param>
/// <param name="conditionRepository">Open journal rows inside the user's visibility scope</param>
/// <param name="dispatchRepository">Which of those findings the user already acknowledged in the inbox</param>
/// <param name="preferenceService">Per-user mute, snooze and minimum-severity filter</param>
/// <param name="logger">Warning sink; a failed resolve yields null instead of an exception</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Interfaces.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Services.Assistant;

public class WelcomeFocusResolver : IWelcomeFocusResolver
{
    private const int JournalTake = 20;

    private static readonly DateTime FreshSetupDetectedAtUtc = DateTime.MinValue;

    private readonly IAgentConditionScopeResolver _scopeResolver;
    private readonly IScheduleActivityProbe _setupProbe;
    private readonly IAgentConditionRepository _conditionRepository;
    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IAgentTriggerPreferenceService _preferenceService;
    private readonly ILogger<WelcomeFocusResolver> _logger;

    public WelcomeFocusResolver(
        IAgentConditionScopeResolver scopeResolver,
        IScheduleActivityProbe setupProbe,
        IAgentConditionRepository conditionRepository,
        IProactiveTriggerDispatchRepository dispatchRepository,
        IAgentTriggerPreferenceService preferenceService,
        ILogger<WelcomeFocusResolver> logger)
    {
        _scopeResolver = scopeResolver;
        _setupProbe = setupProbe;
        _conditionRepository = conditionRepository;
        _dispatchRepository = dispatchRepository;
        _preferenceService = preferenceService;
        _logger = logger;
    }

    public async Task<WelcomeFocusResource?> ResolveAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var scope = await _scopeResolver.ResolveAsync(userId, cancellationToken);
            if (!scope.IsPlanner)
            {
                return null;
            }

            var candidates = await CollectCandidatesAsync(scope, cancellationToken);
            var survivors = await ApplyFiltersAsync(userId, candidates, cancellationToken);
            if (survivors.Count == 0)
            {
                return null;
            }

            var ordered = survivors
                .OrderBy(candidate => WelcomeFocusPriority.Rank(candidate.Kind))
                .ThenBy(candidate => WelcomeFocusPriority.SeverityRank(candidate.Severity))
                .ThenBy(candidate => candidate.DetectedAtUtc);

            foreach (var candidate in ordered)
            {
                var focus = TryMap(candidate, survivors.Count);
                if (focus != null)
                {
                    return focus;
                }
            }

            return null;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Resolving the welcome focus failed for user {UserId}. The welcome continues without a focus question.",
                userId);
            return null;
        }
    }

    private async Task<List<WelcomeFocusCandidate>> CollectCandidatesAsync(
        AgentConditionVisibilityScope scope,
        CancellationToken cancellationToken)
    {
        var candidates = new List<WelcomeFocusCandidate>();

        var setupState = await _setupProbe.GetSetupStateAsync(cancellationToken);
        if (!setupState.HasWork)
        {
            candidates.Add(new WelcomeFocusCandidate(
                AgentTriggerKinds.NoScheduleYet,
                AgentTriggerSeverity.Medium,
                FreshSetupDetectedAtUtc,
                null,
                string.Empty,
                ScheduleSetupStages.For(setupState)));
        }

        var conditions = await _conditionRepository.GetOpenForScopeAsync(
            scope.IsUnrestricted, scope.VisibleRootIds, JournalTake, cancellationToken);

        foreach (var condition in conditions)
        {
            if (string.Equals(condition.TriggerKind, AgentTriggerKinds.NoScheduleYet, StringComparison.Ordinal))
            {
                continue;
            }

            candidates.Add(new WelcomeFocusCandidate(
                condition.TriggerKind,
                condition.Severity,
                condition.DetectedAtUtc,
                condition.Id,
                condition.PayloadJson,
                null));
        }

        return candidates;
    }

    private async Task<List<WelcomeFocusCandidate>> ApplyFiltersAsync(
        string userId,
        List<WelcomeFocusCandidate> candidates,
        CancellationToken cancellationToken)
    {
        var conditionIds = candidates
            .Where(candidate => candidate.ConditionId != null)
            .Select(candidate => candidate.ConditionId!.Value)
            .ToList();

        var acknowledged = await _dispatchRepository.GetAcknowledgedConditionIdsAsync(
            userId, conditionIds, cancellationToken);

        var survivors = new List<WelcomeFocusCandidate>();
        var allowedByKindAndSeverity = new Dictionary<(string Kind, string Severity), bool>();

        foreach (var candidate in candidates)
        {
            if (candidate.ConditionId != null && acknowledged.Contains(candidate.ConditionId.Value))
            {
                continue;
            }

            var key = (candidate.Kind, candidate.Severity);
            if (!allowedByKindAndSeverity.TryGetValue(key, out var isAllowed))
            {
                isAllowed = await _preferenceService.IsAllowedAsync(userId, candidate.Kind, candidate.Severity);
                allowedByKindAndSeverity[key] = isAllowed;
            }

            if (isAllowed)
            {
                survivors.Add(candidate);
            }
        }

        return survivors;
    }

    private static WelcomeFocusResource? TryMap(WelcomeFocusCandidate candidate, int survivingCount)
    {
        if (candidate.Stage != null)
        {
            return MapSetup(candidate.Stage.Value);
        }

        return candidate.Kind switch
        {
            AgentTriggerKinds.PeriodOverdue => MapPeriod(
                candidate,
                WelcomeFocusI18nKeys.PeriodOverduePrompt,
                WelcomeFocusI18nKeys.PeriodOverdueAction,
                ProactiveActionRoutes.PeriodClosing,
                WelcomeFocusPayloadKeys.PeriodEndDate,
                WelcomeFocusParamKeys.PeriodEnd,
                WelcomeFocusPayloadKeys.DaysOverdue),
            AgentTriggerKinds.PeriodCloseDue => MapPeriod(
                candidate,
                WelcomeFocusI18nKeys.PeriodCloseDuePrompt,
                WelcomeFocusI18nKeys.PeriodCloseDueAction,
                ProactiveActionRoutes.PeriodClosing,
                WelcomeFocusPayloadKeys.PeriodEndDate,
                WelcomeFocusParamKeys.PeriodEnd,
                WelcomeFocusPayloadKeys.DaysUntilDue),
            AgentTriggerKinds.NextPeriodSchedulingDue => MapPeriod(
                candidate,
                WelcomeFocusI18nKeys.NextPeriodPrompt,
                WelcomeFocusI18nKeys.NextPeriodAction,
                ProactiveActionRoutes.Schedule,
                WelcomeFocusPayloadKeys.PeriodStartDate,
                WelcomeFocusParamKeys.PeriodStart,
                WelcomeFocusPayloadKeys.DaysUntilStart),
            _ => MapGeneric(candidate, survivingCount)
        };
    }

    private static WelcomeFocusResource MapSetup(ScheduleSetupStage stage)
    {
        var promptKey = stage switch
        {
            ScheduleSetupStage.NothingYet => WelcomeFocusI18nKeys.NoOrdersPrompt,
            ScheduleSetupStage.OrdersButNoShifts => WelcomeFocusI18nKeys.NoShiftsPrompt,
            _ => WelcomeFocusI18nKeys.NoWorkPrompt
        };

        return new WelcomeFocusResource
        {
            Kind = AgentTriggerKinds.NoScheduleYet,
            PromptKey = promptKey,
            PromptParams = new Dictionary<string, string>(StringComparer.Ordinal),
            ActionKind = WelcomeFocusActionKinds.Consultation,
            ActionLabelKey = WelcomeFocusI18nKeys.SetupConsultationAction,
            ActionRoute = null,
            ConditionId = null
        };
    }

    private static WelcomeFocusResource? MapPeriod(
        WelcomeFocusCandidate candidate,
        string promptKey,
        string actionLabelKey,
        string actionRoute,
        string datePayloadKey,
        string dateParamKey,
        string daysPayloadKey)
    {
        try
        {
            using var document = JsonDocument.Parse(candidate.PayloadJson);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!root.TryGetProperty(WelcomeFocusPayloadKeys.GroupName, out var groupElement)
                || !root.TryGetProperty(datePayloadKey, out var dateElement)
                || !root.TryGetProperty(daysPayloadKey, out var daysElement))
            {
                return null;
            }

            var groupName = groupElement.ValueKind == JsonValueKind.String ? groupElement.GetString() : null;
            var date = dateElement.ValueKind == JsonValueKind.String ? dateElement.GetString() : null;
            if (string.IsNullOrWhiteSpace(groupName)
                || string.IsNullOrWhiteSpace(date)
                || daysElement.ValueKind != JsonValueKind.Number
                || !daysElement.TryGetInt32(out var days))
            {
                return null;
            }

            return new WelcomeFocusResource
            {
                Kind = candidate.Kind,
                PromptKey = promptKey,
                PromptParams = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [WelcomeFocusParamKeys.Group] = groupName,
                    [dateParamKey] = date,
                    [WelcomeFocusParamKeys.Days] = days.ToString(CultureInfo.InvariantCulture)
                },
                ActionKind = WelcomeFocusActionKinds.Navigate,
                ActionLabelKey = actionLabelKey,
                ActionRoute = actionRoute,
                ConditionId = candidate.ConditionId
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static WelcomeFocusResource MapGeneric(WelcomeFocusCandidate candidate, int survivingCount)
    {
        return new WelcomeFocusResource
        {
            Kind = candidate.Kind,
            PromptKey = WelcomeFocusI18nKeys.GenericPrompt,
            PromptParams = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [WelcomeFocusParamKeys.Count] = survivingCount.ToString(CultureInfo.InvariantCulture)
            },
            ActionKind = WelcomeFocusActionKinds.Navigate,
            ActionLabelKey = WelcomeFocusI18nKeys.GenericAction,
            ActionRoute = AgentConditionActionRoutes.For(candidate.Kind) ?? ProactiveActionRoutes.Schedule,
            ConditionId = candidate.ConditionId
        };
    }

    private sealed record WelcomeFocusCandidate(
        string Kind,
        string Severity,
        DateTime DetectedAtUtc,
        Guid? ConditionId,
        string PayloadJson,
        ScheduleSetupStage? Stage);
}
