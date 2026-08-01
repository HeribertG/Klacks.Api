// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the export formats together with the adjustment stored for each one, so the agent knows
/// which formatKey values exist and which of them already deviate from the delivered definition.
/// </summary>
/// <param name="onlyAdjusted">When true, formats without a stored adjustment are left out.</param>

using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_export_format_overrides")]
public class ListExportFormatOverridesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListExportFormatOverridesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var catalog = await _mediator.Send(new ListExportFormatOverridesQuery(), cancellationToken);

        var onlyAdjusted = GetParameter<bool?>(parameters, "onlyAdjusted") ?? false;

        var formats = catalog.Formats
            .Where(f => !onlyAdjusted || f.Override != null)
            .Select(f => new
            {
                f.FormatKey,
                f.Family,
                f.AllowedKeys,
                HasOverride = f.Override != null,
                f.Override?.IsEnabled,
                f.Override?.PatchJson,
                f.Override?.Note,
                f.Override?.CreatedUnderVersion,
                f.Override?.UpdateTime
            })
            .ToList();

        var adjusted = catalog.Formats.Count(f => f.Override != null);

        return SkillResult.SuccessResult(
            new { catalog.CurrentVersion, Count = formats.Count, AdjustedCount = adjusted, Formats = formats },
            $"{formats.Count} export format(s) listed, {adjusted} with a stored adjustment.");
    }
}
