// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the applied company rules from the registry: name, kind, when they were applied and their
/// target. Read-only; takes no parameters.
/// </summary>

using Klacks.Api.Application.DTOs.Settings;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills.CompanyRules;

[SkillImplementation("list_company_rules")]
public class ListCompanyRulesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListCompanyRulesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rules = (await _mediator.Send(new ListQuery<CompanyRuleResource>(), cancellationToken) ?? []).ToList();

        var list = rules.Select(r => new
        {
            r.Id,
            r.Name,
            Kind = r.Kind.ToString(),
            r.RuleText,
            r.TargetEntityType,
            r.TargetEntityId,
            r.AppliedUtc
        }).ToList();

        var message = list.Count == 0
            ? "No company rules have been applied yet."
            : $"{list.Count} applied company rule(s).";

        return SkillResult.SuccessResult(new { Count = list.Count, Rules = list }, message);
    }
}
