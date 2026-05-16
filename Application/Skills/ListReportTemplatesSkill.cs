// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists configured report templates, optionally filtered by report type
/// (Schedule / Client / Invoice / Absence). Klacksy uses this to suggest a template
/// before triggering generate / export / email.
/// </summary>
/// <param name="type">Optional. Schedule / Client / Invoice / Absence. All when omitted.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_report_templates")]
public class ListReportTemplatesSkill : BaseSkillImplementation
{
    private readonly IReportTemplateRepository _templateRepository;

    public ListReportTemplatesSkill(IReportTemplateRepository templateRepository)
    {
        _templateRepository = templateRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var typeStr = GetParameter<string>(parameters, "type");

        IEnumerable<Klacks.Api.Domain.Models.Reports.ReportTemplate> templates;
        if (!string.IsNullOrWhiteSpace(typeStr))
        {
            if (!Enum.TryParse<ReportType>(typeStr, true, out var type))
            {
                return SkillResult.Error($"Invalid report type: {typeStr}. Expected Schedule / Client / Invoice / Absence.");
            }
            templates = await _templateRepository.GetByTypeAsync(type, cancellationToken);
        }
        else
        {
            templates = await _templateRepository.GetAllAsync(cancellationToken);
        }

        var rows = templates
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                Type = t.Type.ToString(),
                DataSets = t.DataSetIds
            })
            .OrderBy(r => r.Type)
            .ThenBy(r => r.Name)
            .ToList();

        return SkillResult.SuccessResult(
            new { Templates = rows, TotalCount = rows.Count, FilterType = typeStr },
            $"Found {rows.Count} report template(s)" +
            (!string.IsNullOrWhiteSpace(typeStr) ? $" of type {typeStr}." : "."));
    }
}
