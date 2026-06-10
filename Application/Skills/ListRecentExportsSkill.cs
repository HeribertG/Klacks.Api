// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists completed order export runs from the export log via GetExportLogQuery, newest first.
/// The date range filters on the exported data period (StartDate/EndDate overlap), not on the
/// moment the export was executed. Defaults to the last 365 days when no range is given.
/// </summary>
/// <param name="fromDate">Optional. ISO date yyyy-MM-dd, lower bound of the exported data period.</param>
/// <param name="untilDate">Optional. ISO date yyyy-MM-dd, upper bound of the exported data period.</param>

using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_recent_exports")]
public class ListRecentExportsSkill : BaseSkillImplementation
{
    private const int DefaultLookbackDays = 365;

    private readonly IMediator _mediator;

    public ListRecentExportsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate");

        var to = untilDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var from = fromDate ?? to.AddDays(-DefaultLookbackDays);

        if (from > to)
        {
            return SkillResult.Error("fromDate must not be after untilDate.");
        }

        var entries = await _mediator.Send(new GetExportLogQuery(from, to), cancellationToken);

        var projected = entries
            .Select(e => new
            {
                e.Format,
                e.StartDate,
                e.EndDate,
                e.GroupName,
                e.FileName,
                e.FileSize,
                e.RecordCount,
                e.ExportedAt,
                ExportedBy = !string.IsNullOrWhiteSpace(e.ExportedByName) ? e.ExportedByName : e.ExportedBy
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { Count = projected.Count, From = from, To = to, Exports = projected },
            $"Found {projected.Count} export(s) for data periods between {from:yyyy-MM-dd} and {to:yyyy-MM-dd}.");
    }
}
