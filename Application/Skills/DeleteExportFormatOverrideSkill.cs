// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes the stored adjustment of one export format via DeleteExportFormatOverrideCommand, which
/// reports success as a boolean. The format itself is not deleted — only its deviation from the
/// delivered definition.
/// </summary>
/// <param name="formatKey">Key of the export format whose adjustment is removed (required).</param>

using Klacks.Api.Application.Commands.Exports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_export_format_override")]
public class DeleteExportFormatOverrideSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteExportFormatOverrideSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var formatKey = GetParameter<string>(parameters, "formatKey")?.Trim();
        if (string.IsNullOrWhiteSpace(formatKey))
        {
            return SkillResult.Error("Parameter 'formatKey' is required.");
        }

        var removed = await _mediator.Send(new DeleteExportFormatOverrideCommand(formatKey), cancellationToken);

        if (!removed)
        {
            return SkillResult.Error($"Export format '{formatKey}' has no stored adjustment to remove.");
        }

        return SkillResult.SuccessResult(
            new { FormatKey = formatKey },
            $"Adjustment for export format '{formatKey}' removed; the delivered definition applies again.");
    }
}
