// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates a report template: loads it via GetReportTemplateByIdQuery, patches only the supplied
/// fields and persists via UpdateReportTemplateCommand. Page setup and layout blocks are carried
/// over untouched, so a rename can never wipe a layout.
/// </summary>
/// <param name="reportTemplateId">UUID of the template to update (required).</param>
/// <param name="name">Optional new name.</param>
/// <param name="description">Optional new description.</param>
/// <param name="type">Optional new subject: Schedule, Client, Invoice or Absence.</param>
/// <param name="mergeRows">Optional new setting for merging consecutive identical rows.</param>
/// <param name="showFullPeriod">Optional new setting for showing the full period.</param>

using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Application.Queries.Reports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_report_template")]
public class UpdateReportTemplateSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateReportTemplateSkill(IMediator mediator)
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

        var changed = new List<string>();

        var name = GetParameter<string>(parameters, "name");
        if (!string.IsNullOrWhiteSpace(name) && name.Trim() != existing.Name)
        {
            existing.Name = name.Trim();
            changed.Add("name");
        }

        var description = GetParameter<string>(parameters, "description");
        if (description != null && description.Trim() != existing.Description)
        {
            existing.Description = description.Trim();
            changed.Add("description");
        }

        var typeRaw = GetParameter<string>(parameters, "type");
        if (!string.IsNullOrWhiteSpace(typeRaw))
        {
            if (!Enum.TryParse<ReportType>(typeRaw, ignoreCase: true, out var type) || !Enum.IsDefined(type))
            {
                return SkillResult.Error(
                    $"Invalid type '{typeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<ReportType>())}.");
            }

            if (type != existing.Type)
            {
                existing.Type = type;
                changed.Add("type");
            }
        }

        var mergeRows = GetParameter<bool?>(parameters, "mergeRows");
        if (mergeRows.HasValue && mergeRows.Value != existing.MergeRows)
        {
            existing.MergeRows = mergeRows.Value;
            changed.Add("mergeRows");
        }

        var showFullPeriod = GetParameter<bool?>(parameters, "showFullPeriod");
        if (showFullPeriod.HasValue && showFullPeriod.Value != existing.ShowFullPeriod)
        {
            existing.ShowFullPeriod = showFullPeriod.Value;
            changed.Add("showFullPeriod");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { ReportTemplateId = reportTemplateId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — report template left unchanged.");
        }

        ReportTemplate updated;
        try
        {
            updated = await _mediator.Send(new UpdateReportTemplateCommand(existing), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        return SkillResult.SuccessResult(
            new
            {
                ReportTemplateId = reportTemplateId,
                ChangedFields = changed,
                updated.Name,
                updated.Description,
                Type = updated.Type.ToString()
            },
            $"Report template '{updated.Name}' updated ({string.Join(", ", changed)}).");
    }
}
