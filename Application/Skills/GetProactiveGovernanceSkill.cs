// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports how far Klacksy is currently allowed to act by itself per trigger kind, plus the global
/// kill switch. Read-only on purpose: the matching setting skill is classified sensitive so that
/// Klacksy can describe its own leash but never loosen it.
/// </summary>
/// <param name="mediator">Dispatches the governance query.</param>

using Klacks.Api.Application.Queries.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_proactive_governance")]
public class GetProactiveGovernanceSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetProactiveGovernanceSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var governance = await _mediator.Send(new GetProactiveGovernanceQuery(), cancellationToken);

        var levelNames = new[] { "Propose (report only)", "Assisted", "Autonomous", "FullyAutonomous" };
        var levelName = governance.GlobalAutonomyLevel is >= 0 and <= 3
            ? levelNames[governance.GlobalAutonomyLevel]
            : governance.GlobalAutonomyLevel.ToString();

        var summary = governance.KillSwitchActive
            ? $"The global kill switch is ON, so all {governance.Rules.Count} trigger kinds are pinned to Hint. " +
              $"The global autonomy level is {governance.GlobalAutonomyLevel} ({levelName})."
            : $"Governance for {governance.Rules.Count} trigger kinds; the global kill switch is off. " +
              $"The global autonomy level is {governance.GlobalAutonomyLevel} ({levelName}), capping every kind's maxAction.";

        return SkillResult.SuccessResult(governance, summary);
    }
}
