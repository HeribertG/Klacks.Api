// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the default ERP drop point via GetDefaultQuery: name, source system, bucket prefix, whether
/// it is switched on, when it was last polled and the last error it reported.
/// </summary>

using Klacks.Api.Application.Queries.ErpDropPoints;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_erp_drop_point_settings")]
public class GetErpDropPointSettingsSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public GetErpDropPointSettingsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var dropPoint = await _mediator.Send(new GetDefaultQuery(), cancellationToken);

        if (dropPoint == null)
        {
            return SkillResult.Error("No default drop point is configured.");
        }

        return SkillResult.SuccessResult(
            new
            {
                dropPoint.Id,
                dropPoint.Name,
                dropPoint.SourceSystemId,
                dropPoint.BucketPrefix,
                dropPoint.IsEnabled,
                dropPoint.LastPolledAt,
                dropPoint.LastError
            },
            $"Default drop point '{dropPoint.Name}' ({(dropPoint.IsEnabled ? "switched on" : "switched off")}).");
    }
}
