// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reverts a previously applied company rule identified by name. Resolves the name unambiguously against
/// the registry (fuzzy; ambiguous or unknown names return the candidate list) and dispatches
/// <see cref="RevertCompanyRuleCommand"/>, which restores the surcharge snapshot or soft-deletes the
/// created counter rule / macro and the registry row. Real failures are relayed verbatim. Human
/// confirmation is enforced by the autonomy gate.
/// </summary>
/// <param name="name">Required. The name of the applied company rule to revert.</param>

using Klacks.Api.Application.Commands.CompanyRules;
using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills.CompanyRules;

[SkillImplementation("revert_company_rule")]
public class RevertCompanyRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public RevertCompanyRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name");
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("name is required — pass the name of the applied company rule to revert.");
        }

        var rules = (await _mediator.Send(new ListQuery<CompanyRuleResource>(), cancellationToken) ?? []).ToList();
        var (rule, error) = CompanyRuleResolver.Resolve(rules, name);
        if (rule is null)
        {
            return SkillResult.Error(error!);
        }

        try
        {
            var reverted = await _mediator.Send(new RevertCompanyRuleCommand(rule.Id), cancellationToken);
            if (reverted is null)
            {
                return SkillResult.Error($"Company rule '{rule.Name}' could not be reverted.");
            }

            return SkillResult.SuccessResult(
                new { reverted.Id, reverted.Name, Kind = reverted.Kind.ToString() },
                $"Company rule '{reverted.Name}' ({reverted.Kind}) reverted.");
        }
        catch (InvalidRequestException ex)
        {
            return SkillResult.Error(ex.Message);
        }
    }
}
