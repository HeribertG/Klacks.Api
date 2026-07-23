// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the in-progress planning-profile draft: dispatches <see cref="ApplyPlanningProfileCommand"/>,
/// which copies the base industry's template rules as customer-owned rules (or creates one blank rule for
/// a scratch base), applies the field overrides, sets ACTIVE_INDUSTRIES to the custom marker and clears
/// the draft. Domain problems (no draft, missing required parameters) are relayed verbatim so the model
/// can correct them. Human confirmation is enforced by the autonomy gate.
/// </summary>

using Klacks.Api.Application.Commands.PlanningProfile;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills.PlanningProfile;

[SkillImplementation("apply_planning_profile")]
public class ApplyPlanningProfileSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ApplyPlanningProfileSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = PlanningProfileDraftScope.ConversationKey(context);

        try
        {
            var applied = await _mediator.Send(
                new ApplyPlanningProfileCommand(context.UserId, conversationKey), cancellationToken);

            if (applied is null)
            {
                return SkillResult.Error("The planning profile could not be applied.");
            }

            var ruleSummary = applied.CreatedRuleCount == 0
                ? "no scheduling rules were created"
                : $"{applied.CreatedRuleCount} customer-owned scheduling rule(s) created ({string.Join(", ", applied.CreatedRuleNames)})";

            return SkillResult.SuccessResult(
                applied,
                $"Custom planning profile applied from base '{applied.BaseChoice}': {ruleSummary}. " +
                $"ACTIVE_INDUSTRIES is now '{applied.ActiveIndustries}'.");
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }
    }
}
