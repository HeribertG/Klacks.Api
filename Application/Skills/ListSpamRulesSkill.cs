// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the individual spam rules via GetSpamRulesQuery so the agent can pick a spamRuleId before
/// update_spam_rule / delete_spam_rule.
/// </summary>

using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_spam_rules")]
public class ListSpamRulesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListSpamRulesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var rules = await _mediator.Send(new GetSpamRulesQuery(), cancellationToken);

        var projected = rules
            .OrderBy(r => r.SortOrder)
            .Select(r => new
            {
                r.Id,
                RuleType = r.RuleType.ToString(),
                r.Pattern,
                r.IsActive,
                r.SortOrder
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, SpamRules = projected },
            $"Found {projected.Count} spam rule(s).");
    }
}
