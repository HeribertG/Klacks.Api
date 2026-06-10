// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reads the dashboard's current-month shift-coverage statistics and condenses them into
/// chat-friendly key figures: covered vs. open slots, coverage ratio and sealing progress
/// (confirmed work entries) per group plus overall totals. Groups are ordered worst-covered
/// first. For the year-long capacity-vs-demand view use interpret_resource_monitor; for the
/// employee location map use get_client_locations_overview.
/// </summary>

using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_dashboard_summary")]
public class GetDashboardSummarySkill : BaseSkillImplementation
{
    private const int RatioDecimals = 2;
    private const string MonthFormat = "yyyy-MM";

    private readonly IMediator _mediator;

    public GetDashboardSummarySkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var statistics = await _mediator.Send(new GetShiftCoverageStatisticsQuery(), cancellationToken);
        var groups = statistics?.ToList() ?? [];

        var perGroup = groups
            .Select(g => new
            {
                g.GroupId,
                g.GroupName,
                g.TotalSlots,
                g.CoveredSlots,
                OpenSlots = g.TotalSlots - g.CoveredSlots,
                CoverageRatio = Ratio(g.CoveredSlots, g.TotalSlots),
                g.TotalWorkEntries,
                g.SealedWorkEntries,
                SealedRatio = Ratio(g.SealedWorkEntries, g.TotalWorkEntries)
            })
            .OrderBy(g => g.CoverageRatio ?? double.MaxValue)
            .ThenBy(g => g.GroupName)
            .ToList();

        var totalSlots = groups.Sum(g => g.TotalSlots);
        var coveredSlots = groups.Sum(g => g.CoveredSlots);
        var totalWorkEntries = groups.Sum(g => g.TotalWorkEntries);
        var sealedWorkEntries = groups.Sum(g => g.SealedWorkEntries);
        var month = DateTime.UtcNow.ToString(MonthFormat);

        var data = new
        {
            Month = month,
            GroupCount = groups.Count,
            TotalSlots = totalSlots,
            CoveredSlots = coveredSlots,
            OpenSlots = totalSlots - coveredSlots,
            CoverageRatio = Ratio(coveredSlots, totalSlots),
            TotalWorkEntries = totalWorkEntries,
            SealedWorkEntries = sealedWorkEntries,
            SealedRatio = Ratio(sealedWorkEntries, totalWorkEntries),
            Groups = perGroup
        };

        var message =
            $"Dashboard summary for {month}: {coveredSlots}/{totalSlots} slots covered across " +
            $"{groups.Count} group(s), {totalSlots - coveredSlots} open, " +
            $"{sealedWorkEntries}/{totalWorkEntries} work entries sealed.";

        return SkillResult.SuccessResult(data, message);
    }

    private static double? Ratio(int part, int total)
        => total > 0 ? Math.Round((double)part / total, RatioDecimals) : null;
}
