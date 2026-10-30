// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises on whether to install the offered update now: versions, channel, whether the trigger would accept it, a failed
/// last attempt, a schema change, running planning work and other connected users. Read-only advice computed in C#
/// (SoftwareUpdateAdvisor); installing is a separate step the system confirms once (the AdvisesFor target is Sensitive).
/// </summary>

using Klacks.Api.Application.Queries.Update;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateSoftwareUpdateSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_software_update";

    private readonly IMediator _mediator;

    public EvaluateSoftwareUpdateSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var evaluation = await _mediator.Send(new EvaluateSoftwareUpdateQuery(context.UserId.ToString()), cancellationToken);
        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }
}
