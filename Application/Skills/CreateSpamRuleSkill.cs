// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a single spam rule via CreateSpamRuleCommand. The rule type is parsed case-insensitively
/// and rejected with the list of accepted values, so an unknown word never silently becomes
/// SenderContains.
/// </summary>
/// <param name="ruleType">Where the text is looked for: SenderContains, SenderDomain, SubjectContains or BodyContains.</param>
/// <param name="pattern">Text fragment or domain the rule looks for (required).</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_spam_rule")]
public class CreateSpamRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateSpamRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var ruleTypeRaw = GetParameter<string>(parameters, "ruleType");
        if (!Enum.TryParse<SpamRuleType>(ruleTypeRaw, ignoreCase: true, out var ruleType)
            || !Enum.IsDefined(ruleType))
        {
            return SkillResult.Error(
                $"Invalid ruleType '{ruleTypeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<SpamRuleType>())}.");
        }

        var pattern = GetParameter<string>(parameters, "pattern")?.Trim();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return SkillResult.Error("Parameter 'pattern' is required.");
        }

        SpamRuleResource created;
        try
        {
            created = await _mediator.Send(new CreateSpamRuleCommand(ruleType, pattern), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        return SkillResult.SuccessResult(
            new { created.Id, RuleType = created.RuleType.ToString(), created.Pattern, created.IsActive, created.SortOrder },
            $"Spam rule '{created.Pattern}' ({created.RuleType}) created (id {created.Id}).");
    }
}
