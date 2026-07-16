// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Applies the in-progress company-rule draft: dispatches <see cref="ApplyCompanyRuleCommand"/>, which
/// writes the target entity, records the registry row and clears the draft. Domain problems (missing
/// parameters, ambiguous scheduling rule, macro name collision, invalid macro script) are relayed
/// verbatim so the model can correct them. Human confirmation is enforced by the autonomy gate.
/// </summary>

using Klacks.Api.Application.Commands.CompanyRules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills.CompanyRules;

[SkillImplementation("apply_company_rule")]
public class ApplyCompanyRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ApplyCompanyRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var conversationKey = CompanyRuleDraftScope.ConversationKey(context);

        try
        {
            var applied = await _mediator.Send(
                new ApplyCompanyRuleCommand(context.UserId, conversationKey), cancellationToken);

            if (applied is null)
            {
                return SkillResult.Error("The company rule could not be applied.");
            }

            return SkillResult.SuccessResult(
                new { applied.Id, applied.Name, Kind = applied.Kind.ToString(), applied.TargetEntityType, applied.TargetEntityId },
                $"Company rule '{applied.Name}' ({applied.Kind}) applied. Target: {applied.TargetEntityType}" +
                (applied.TargetEntityId.HasValue ? $" ({applied.TargetEntityId})." : "."));
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }
    }
}
