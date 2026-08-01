// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a report template via CreateReportTemplateCommand with its page setup left at the default
/// and no layout blocks. The blocks carry positions and bindings that no flat parameter list can
/// express, so they are arranged afterwards on the reports page.
/// </summary>
/// <param name="name">Name of the template (required).</param>
/// <param name="type">What the template reports on: Schedule, Client, Invoice or Absence.</param>
/// <param name="description">Optional description.</param>
/// <param name="mergeRows">Whether consecutive identical rows are merged.</param>
/// <param name="showFullPeriod">Whether the full period is shown rather than only days with content.</param>

using Klacks.Api.Application.Commands.Reports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Reports;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("create_report_template")]
public class CreateReportTemplateSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CreateReportTemplateSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetParameter<string>(parameters, "name")?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return SkillResult.Error("Parameter 'name' is required.");
        }

        var typeRaw = GetParameter<string>(parameters, "type");
        if (!Enum.TryParse<ReportType>(typeRaw, ignoreCase: true, out var type) || !Enum.IsDefined(type))
        {
            return SkillResult.Error(
                $"Invalid type '{typeRaw}'. Use one of: {string.Join(", ", Enum.GetNames<ReportType>())}.");
        }

        var template = new ReportTemplate
        {
            Name = name,
            Description = GetParameter<string>(parameters, "description")?.Trim() ?? string.Empty,
            Type = type,
            MergeRows = GetParameter<bool?>(parameters, "mergeRows") ?? false,
            ShowFullPeriod = GetParameter<bool?>(parameters, "showFullPeriod") ?? false
        };

        ReportTemplate created;
        try
        {
            created = await _mediator.Send(new CreateReportTemplateCommand(template), cancellationToken);
        }
        catch (InvalidRequestException exception)
        {
            return SkillResult.Error(exception.Message);
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.Name, created.Description, Type = created.Type.ToString() },
            $"Report template '{created.Name}' created (id {created.Id}). It has no layout blocks yet.");
    }
}
