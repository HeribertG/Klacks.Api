// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Computes, for every skill risk class, whether it needs confirmation on the chat gate
/// (AutonomyGateService.IsAllowed) and the plan-step gate (PlanStepApprovalPolicy.RequiresApproval)
/// at the user's current level vs. a proposed target level, against the live skill registry.
/// Every number here is computed, never guessed by the caller.
/// </summary>
/// <param name="preferenceRepository">Per-user autonomy preference storage, for the current level</param>
/// <param name="skillRegistry">Live skill catalogue, to count skills per risk class</param>
/// <param name="riskClassifier">Classifies a skill descriptor into its SkillRiskClass</param>

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Application.Services.Assistant.Autonomy;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Assistant;

public class EvaluateAutonomyLevelChangeQueryHandler
    : IRequestHandler<EvaluateAutonomyLevelChangeQuery, AutonomyLevelChangeEvaluationResult>
{
    private static readonly SkillRiskClass[] AllRiskClasses =
    [
        SkillRiskClass.ReadOnly,
        SkillRiskClass.Reversible,
        SkillRiskClass.ScenarioGated,
        SkillRiskClass.Irreversible,
        SkillRiskClass.Sensitive
    ];

    private readonly IAgentAutonomyPreferenceRepository _preferenceRepository;
    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillRiskClassifier _riskClassifier;

    public EvaluateAutonomyLevelChangeQueryHandler(
        IAgentAutonomyPreferenceRepository preferenceRepository,
        ISkillRegistry skillRegistry,
        ISkillRiskClassifier riskClassifier)
    {
        _preferenceRepository = preferenceRepository;
        _skillRegistry = skillRegistry;
        _riskClassifier = riskClassifier;
    }

    public async Task<AutonomyLevelChangeEvaluationResult> Handle(
        EvaluateAutonomyLevelChangeQuery request, CancellationToken cancellationToken)
    {
        var row = await _preferenceRepository.GetAsync(request.UserId.ToString(), cancellationToken);
        var currentLevel = row?.Level ?? AutonomyDefaults.DefaultLevel;
        var targetLevel = request.TargetLevel;

        var countByRiskClass = _skillRegistry.GetAllSkills()
            .GroupBy(_riskClassifier.Classify)
            .ToDictionary(g => g.Key, g => g.Count());

        var impacts = AllRiskClasses
            .Select(riskClass => BuildImpact(riskClass, countByRiskClass, currentLevel, targetLevel))
            .ToList();

        var newlyUnconfirmed = impacts
            .Where(i => i.ChatConfirmationRequiredAtCurrent && !i.ChatConfirmationRequiredAtTarget)
            .Sum(i => i.SkillCount);
        var newlyConfirmed = impacts
            .Where(i => !i.ChatConfirmationRequiredAtCurrent && i.ChatConfirmationRequiredAtTarget)
            .Sum(i => i.SkillCount);

        return new AutonomyLevelChangeEvaluationResult(
            CurrentLevel: (int)currentLevel,
            CurrentLevelName: currentLevel.ToString(),
            TargetLevel: (int)targetLevel,
            TargetLevelName: targetLevel.ToString(),
            IsNoOp: currentLevel == targetLevel,
            IsDowngrade: targetLevel < currentLevel,
            Impacts: impacts,
            SkillsNewlyUnconfirmedInChat: newlyUnconfirmed,
            SkillsNewlyConfirmedInChat: newlyConfirmed,
            Recommendation: BuildRecommendation(currentLevel, targetLevel, newlyUnconfirmed, newlyConfirmed, impacts));
    }

    private static AutonomyRiskClassImpact BuildImpact(
        SkillRiskClass riskClass,
        IReadOnlyDictionary<SkillRiskClass, int> countByRiskClass,
        AutonomyLevel currentLevel,
        AutonomyLevel targetLevel)
    {
        return new AutonomyRiskClassImpact(
            RiskClass: riskClass.ToString(),
            SkillCount: countByRiskClass.GetValueOrDefault(riskClass),
            ChatConfirmationRequiredAtCurrent: !AutonomyGateService.IsAllowed(riskClass, currentLevel),
            ChatConfirmationRequiredAtTarget: !AutonomyGateService.IsAllowed(riskClass, targetLevel),
            PlanApprovalRequiredAtCurrent: PlanStepApprovalPolicy.RequiresApproval(riskClass, currentLevel),
            PlanApprovalRequiredAtTarget: PlanStepApprovalPolicy.RequiresApproval(riskClass, targetLevel));
    }

    private static string BuildRecommendation(
        AutonomyLevel currentLevel,
        AutonomyLevel targetLevel,
        int newlyUnconfirmed,
        int newlyConfirmed,
        IReadOnlyList<AutonomyRiskClassImpact> impacts)
    {
        const string sensitiveNote = " Sensitive actions (e.g. user administration, permission changes, " +
            "changing the autonomy level itself) always keep requiring confirmation, at every level.";

        if (currentLevel == targetLevel)
        {
            return $"No change: already at {currentLevel}.{sensitiveNote}";
        }

        var planChanges = impacts.Where(i => i.PlanBehaviorChanges).Select(i => i.RiskClass).ToList();
        var planNote = planChanges.Count > 0
            ? $" For unattended multi-step plans, {string.Join(", ", planChanges)} step(s) would also stop pausing for approval."
            : string.Empty;

        if (targetLevel > currentLevel)
        {
            return newlyUnconfirmed > 0
                ? $"Raising the level from {currentLevel} to {targetLevel} lets {newlyUnconfirmed} currently " +
                  $"registered skill(s) run without asking first.{planNote}{sensitiveNote}"
                : $"Raising the level from {currentLevel} to {targetLevel} changes no currently registered " +
                  $"skill's confirmation behavior on the chat gate.{planNote}{sensitiveNote}";
        }

        return newlyConfirmed > 0
            ? $"Lowering the level from {currentLevel} to {targetLevel} makes {newlyConfirmed} currently " +
              $"registered skill(s) require confirmation again that currently run without asking.{planNote}{sensitiveNote}"
            : $"Lowering the level from {currentLevel} to {targetLevel} changes no currently registered " +
              $"skill's confirmation behavior on the chat gate.{planNote}{sensitiveNote}";
    }
}
