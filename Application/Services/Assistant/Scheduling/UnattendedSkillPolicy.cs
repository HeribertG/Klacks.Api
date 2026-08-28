// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Decides whether a skill may run unattended on a background path, where nobody can confirm anything
/// and the autonomy gate is bypassed. It fails closed: without frozen owner permissions there is no
/// permission check at all, an unknown skill cannot be classified, a skill that is sensitive
/// <em>today</em> is refused even when it was harmless at the time the schedule was created, an
/// unrecognised risk class is refused rather than waved through, and every remaining class has to clear
/// an autonomy threshold that is stricter than the interactive one. An irreversible skill is refused
/// outright unless a scheduled task carries an explicit per-task opt-in; the proactive heartbeat has no
/// such opt-in and therefore never runs an irreversible skill.
///
/// Every refusal text states the CAUSE and the REMEDY only. What happens to the caller afterwards -
/// pausing a scheduled task, disabling it, or nothing at all on the heartbeat, where no task exists to
/// pause - is decided and worded by that caller, so the same policy text can never claim a consequence
/// the caller did not apply.
/// </summary>
/// <param name="registry">Resolves the skill name to its descriptor</param>
/// <param name="classifier">Yields the current risk class of that descriptor</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Scheduling;

public sealed class UnattendedSkillPolicy : IUnattendedSkillPolicy
{
    private readonly ISkillRegistry _registry;
    private readonly ISkillRiskClassifier _classifier;

    public UnattendedSkillPolicy(ISkillRegistry registry, ISkillRiskClassifier classifier)
    {
        _registry = registry;
        _classifier = classifier;
    }

    public UnattendedSkillDecision Decide(UnattendedSkillRequest request)
    {
        if (request.OwnerPermissions.Count == 0)
        {
            return UnattendedSkillDecision.Deny(
                "The owner of this background run has no permissions at all right now, so there would be " +
                "no permission check to apply. An administrator has to grant the owner the roles the " +
                "skill needs.",
                UnattendedDenyReason.NoPermissions);
        }

        var descriptor = _registry.GetSkillByName(request.SkillName);
        if (descriptor is null)
        {
            return UnattendedSkillDecision.Deny(
                $"Skill '{request.SkillName}' no longer exists, so there is nothing left to run.",
                UnattendedDenyReason.UnknownSkill);
        }

        var riskClass = _classifier.Classify(descriptor);
        return riskClass switch
        {
            SkillRiskClass.ReadOnly => UnattendedSkillDecision.Allow(),
            SkillRiskClass.Reversible => DecideByAutonomyLevel(
                request, riskClass, UnattendedSkillPolicyDefaults.MinimumLevelForReversible),
            SkillRiskClass.ScenarioGated => DecideByAutonomyLevel(
                request, riskClass, UnattendedSkillPolicyDefaults.MinimumLevelForScenarioGated),
            SkillRiskClass.Irreversible => DecideIrreversible(request),
            SkillRiskClass.Sensitive => DenySensitive(request.SkillName),
            _ => UnattendedSkillDecision.Deny(
                $"Skill '{request.SkillName}' has an unrecognised risk class and cannot be judged for a " +
                "background run at all. Run the skill interactively instead.",
                UnattendedDenyReason.UnknownRiskClass)
        };
    }

    private static UnattendedSkillDecision DecideByAutonomyLevel(
        UnattendedSkillRequest request, SkillRiskClass riskClass, AutonomyLevel minimumLevel)
    {
        if (request.AutonomyLevel >= minimumLevel)
        {
            return UnattendedSkillDecision.Allow();
        }

        return UnattendedSkillDecision.Deny(
            $"Skill '{request.SkillName}' is classified as {riskClass} and needs autonomy level " +
            $"{minimumLevel} or higher to run unattended, but the owner is at {request.AutonomyLevel}. " +
            $"Raise the autonomy level to {minimumLevel} or higher.",
            UnattendedDenyReason.AutonomyLevelTooLow);
    }

    private static UnattendedSkillDecision DecideIrreversible(UnattendedSkillRequest request)
    {
        // Email automation is judged by autonomy level rather than by an opt-in it cannot carry. The
        // threshold is the highest one, so this neither loosens the scheduled-task rule (which still
        // needs its explicit per-task flag) nor the heartbeat rule (which still refuses outright).
        if (request.ExecutionKind == UnattendedExecutionKind.EmailAutomation)
        {
            return request.AutonomyLevel >= UnattendedSkillPolicyDefaults.MinimumLevelForIrreversibleEmailAutomation
                ? UnattendedSkillDecision.Allow()
                : UnattendedSkillDecision.Deny(
                    $"Skill '{request.SkillName}' is classified as irreversible and needs autonomy level " +
                    $"{UnattendedSkillPolicyDefaults.MinimumLevelForIrreversibleEmailAutomation} to run " +
                    $"from an incoming email, but the owner is at {request.AutonomyLevel}. Raise the " +
                    $"autonomy level to {UnattendedSkillPolicyDefaults.MinimumLevelForIrreversibleEmailAutomation}.",
                    UnattendedDenyReason.AutonomyLevelTooLow);
        }

        if (request.ExecutionKind != UnattendedExecutionKind.ScheduledTask)
        {
            return UnattendedSkillDecision.Deny(
                $"Skill '{request.SkillName}' is classified as irreversible and never runs unattended on " +
                "the proactive path, where no per-task opt-in exists. Propose the action to a human " +
                "instead of executing it.",
                UnattendedDenyReason.IrreversibleWithoutOptIn);
        }

        if (!request.AllowIrreversibleUnattended)
        {
            return UnattendedSkillDecision.Deny(
                $"Skill '{request.SkillName}' is classified as irreversible and does not run unattended " +
                "unless this task explicitly opts in. Either allow irreversible unattended runs for this " +
                "task or change it to a skill that can be undone.",
                UnattendedDenyReason.IrreversibleWithoutOptIn);
        }

        if (request.AutonomyLevel < UnattendedSkillPolicyDefaults.MinimumLevelForIrreversibleOptIn)
        {
            return UnattendedSkillDecision.Deny(
                $"Skill '{request.SkillName}' is classified as Irreversible and needs autonomy level " +
                $"{UnattendedSkillPolicyDefaults.MinimumLevelForIrreversibleOptIn} or higher to run " +
                $"unattended even with the opt-in, but the owner is at {request.AutonomyLevel}. " +
                $"Raise the autonomy level to {UnattendedSkillPolicyDefaults.MinimumLevelForIrreversibleOptIn} " +
                "or higher.",
                UnattendedDenyReason.AutonomyLevelTooLow);
        }

        return UnattendedSkillDecision.Allow();
    }

    private static UnattendedSkillDecision DenySensitive(string skillName)
    {
        return UnattendedSkillDecision.Deny(
            $"Skill '{skillName}' is now classified as sensitive and must never run unattended. " +
            "Run the skill interactively instead.",
            UnattendedDenyReason.SensitiveSkill);
    }
}
