// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a report template. DeleteReportTemplateCommand returns nothing, so the template is read
/// beforehand to report which one was removed and to distinguish a missing id from a successful
/// deletion.
/// </summary>
/// <param name="reportTemplateId">UUID of the template to delete (required).</param>

using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.Queries.Reports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_report_template")]
public class DeleteReportTemplateSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteReportTemplateSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var reportTemplateId = GetRequiredGuid(parameters, "reportTemplateId");

        var existing = await _mediator.Send(new GetReportTemplateByIdQuery(reportTemplateId), cancellationToken);
        if (existing == null)
        {
            return SkillResult.Error($"Report template {reportTemplateId} not found.");
        }

        await _mediator.Send(new DeleteReportTemplateCommand(reportTemplateId), cancellationToken);

        var stillThere = await _mediator.Send(new GetReportTemplateByIdQuery(reportTemplateId), cancellationToken);
        if (stillThere != null)
        {
            return SkillResult.Error(
                $"Report template '{existing.Name}' is still present after the delete — treat it as not removed.");
        }

        return SkillResult.SuccessResult(
            new { ReportTemplateId = reportTemplateId, existing.Name },
            $"Report template '{existing.Name}' removed and confirmed gone (verified).");
    }
}
