// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill triggering an immediate ERP order import run instead of waiting for the next
/// scheduled tick, e.g. right after a file was dropped manually.
/// </summary>

using Klacks.Api.Application.Commands.Imports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("trigger_erp_import_run")]
public class TriggerErpImportRunSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public TriggerErpImportRunSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new TriggerErpImportRunCommand(), cancellationToken);
        return SkillResult.SuccessResult(null, "ERP import triggered — it runs within the next 60 seconds.");
    }
}
