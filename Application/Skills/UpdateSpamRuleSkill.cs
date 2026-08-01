// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a single spam rule. UpdateSpamRuleCommand carries every field, so the current row is read
/// from GetSpamRulesQuery first and only the supplied fields are replaced — otherwise an unmentioned
/// field would be overwritten with a default.
/// </summary>
/// <param name="spamRuleId">UUID of the spam rule to update (required).</param>
/// <param name="ruleType">Optional new place to look: SenderContains, SenderDomain, SubjectContains or BodyContains.</param>
/// <param name="pattern">Optional new text fragment or domain.</param>
/// <param name="isActive">Optional new active flag.</param>
/// <param name="sortOrder">Optional new position in the evaluation order.</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Application.DTOs.Email;
using Klacks.Api.Application.Queries.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_spam_rule")]
public class UpdateSpamRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateSpamRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var spamRuleId = GetRequiredGuid(parameters, "spamRuleId");

        var rules = await _mediator.Send(new GetSpamRulesQuery(), cancellationToken);
        var existing = rules.FirstOrDefault(r => r.Id == spamRuleId);
        if (existing == null)
        {
            return SkillResult.Error($"Spam rule {spamRuleId} not found.");
        }

        var changed = new List<string>();
        var ruleType = existing.RuleType;
        var pattern = existing.Pattern;
        var isActive = existing.IsActive;
        var sortOrder = existing.SortOrder;

        var ruleTypeRaw = GetParameter<string>(parameters, "ruleType");
        if (!string.IsNullOrWhiteSpace(ruleTypeRaw))
        {
            if (!Enum.TryParse<SpamRuleType>(ruleTypeRaw, ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed))
            {
                return SkillResult.Error(
                    $"Invalid ruleType '{ruleTypeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<SpamRuleType>())}.");
            }

            if (parsed != ruleType)
            {
                ruleType = parsed;
                changed.Add("ruleType");
            }
        }

        var patternRaw = GetParameter<string>(parameters, "pattern");
        if (patternRaw != null && patternRaw.Trim() != pattern)
        {
            pattern = patternRaw.Trim();
            changed.Add("pattern");
        }

        var isActiveRaw = GetParameter<bool?>(parameters, "isActive");
        if (isActiveRaw.HasValue && isActiveRaw.Value != isActive)
        {
            isActive = isActiveRaw.Value;
            changed.Add("isActive");
        }

        var sortOrderRaw = GetParameter<int?>(parameters, "sortOrder");
        if (sortOrderRaw.HasValue && sortOrderRaw.Value != sortOrder)
        {
            sortOrder = sortOrderRaw.Value;
            changed.Add("sortOrder");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { SpamRuleId = spamRuleId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — spam rule left unchanged.");
        }

        SpamRuleResource updated;
        try
        {
            updated = await _mediator.Send(
                new UpdateSpamRuleCommand(spamRuleId, ruleType, pattern, isActive, sortOrder), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        return SkillResult.SuccessResult(
            new
            {
                SpamRuleId = spamRuleId,
                ChangedFields = changed,
                RuleType = updated.RuleType.ToString(),
                updated.Pattern,
                updated.IsActive,
                updated.SortOrder
            },
            $"Spam rule {spamRuleId} updated ({string.Join(", ", changed)}).");
    }
}
