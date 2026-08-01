// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a single spam rule via DeleteSpamRuleCommand, which reports success as a boolean rather
/// than returning the removed row.
/// </summary>
/// <param name="spamRuleId">UUID of the spam rule to delete (required).</param>

using Klacks.Api.Application.Commands.Email;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_spam_rule")]
public class DeleteSpamRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteSpamRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var spamRuleId = GetRequiredGuid(parameters, "spamRuleId");

        var removed = await _mediator.Send(new DeleteSpamRuleCommand(spamRuleId), cancellationToken);

        if (!removed)
        {
            return SkillResult.Error($"Spam rule {spamRuleId} not found.");
        }

        return SkillResult.SuccessResult(
            new { SpamRuleId = spamRuleId },
            $"Spam rule {spamRuleId} removed.");
    }
}
