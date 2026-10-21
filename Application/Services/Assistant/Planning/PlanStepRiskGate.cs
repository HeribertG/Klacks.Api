// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Risk decisions for one plan step, taken from the skill's risk class and never from an LLM-supplied flag on the step:
/// whether the step must pause for approval at the run's autonomy level (PlanStepApprovalPolicy; a skill without a
/// registered descriptor always pauses), and whether its verify skill is a registered READ-ONLY catalogue skill.
/// </summary>
/// <param name="skillRegistry">Resolves the step's and the verify skill's descriptors</param>
/// <param name="riskClassifier">Classifies a descriptor into its SkillRiskClass</param>

using System.Globalization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Planning;

internal sealed class PlanStepRiskGate
{
    private const string VerifySkillUnknownMessage =
        "Verify skill '{0}' of step '{1}' is not a registered skill. A verify step must name a read-only "
        + "skill from the catalogue; the plan was stopped before the step ran.";
    private const string VerifySkillNotReadOnlyMessage =
        "Verify skill '{0}' of step '{1}' is classified {2}, but a verify step must be read-only; the plan "
        + "was stopped before the step ran.";

    private readonly ISkillRegistry _skillRegistry;
    private readonly ISkillRiskClassifier _riskClassifier;

    public PlanStepRiskGate(ISkillRegistry skillRegistry, ISkillRiskClassifier riskClassifier)
    {
        _skillRegistry = skillRegistry;
        _riskClassifier = riskClassifier;
    }

    /// <summary>
    /// Rejects a verify skill that is not a read-only catalogue skill. The name is taken verbatim from the
    /// LLM response (PlanningAgent) and is never matched against the catalogue, while the verify call runs
    /// with the chat autonomy gate bypassed and without any RequiresApproval check - so a sensitive skill
    /// named as verifySkill would execute unattended. The system prompt already defines a verify step as a
    /// pure read of the mutation's result; this enforces that in code instead of trusting the prompt.
    /// An unregistered name fails too, mirroring the fail-closed <c>descriptor == null</c> arm of
    /// <see cref="RequiresApproval"/>. The caller checks this BEFORE the mutation runs: afterwards there is no
    /// safe stopping point left, because resuming a paused plan re-enters the step and would repeat the mutation.
    /// </summary>
    /// <param name="step">Step whose VerifySkill is validated; a step without one is always accepted</param>
    /// <returns>The failure message, or null when the step may run</returns>
    public string? ValidateVerifySkill(PlanStep step)
    {
        if (string.IsNullOrWhiteSpace(step.VerifySkill))
        {
            return null;
        }

        var descriptor = _skillRegistry.GetSkillByName(step.VerifySkill);
        if (descriptor == null)
        {
            return string.Format(
                CultureInfo.InvariantCulture, VerifySkillUnknownMessage, step.VerifySkill, step.Skill);
        }

        var riskClass = _riskClassifier.Classify(descriptor);
        return riskClass == SkillRiskClass.ReadOnly
            ? null
            : string.Format(
                CultureInfo.InvariantCulture, VerifySkillNotReadOnlyMessage, step.VerifySkill, step.Skill, riskClass);
    }

    public bool RequiresApproval(PlanStep step, AutonomyLevel autonomyLevel)
    {
        var descriptor = _skillRegistry.GetSkillByName(step.Skill);
        if (descriptor == null)
        {
            return true;
        }

        return PlanStepApprovalPolicy.RequiresApproval(_riskClassifier.Classify(descriptor), autonomyLevel);
    }
}
