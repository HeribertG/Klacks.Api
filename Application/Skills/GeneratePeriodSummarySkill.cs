// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Generates a textual period summary (employee count, total target hours, computed hours
/// and drift) for a given group + period. Klacksy uses this for chat answers like
/// "give me the May summary for Bern" or before triggering email_schedule_to_client.
/// </summary>
/// <param name="groupId">Required. UUID of the group / location.</param>
/// <param name="fromDate">Required. ISO date yyyy-MM-dd (period start).</param>
/// <param name="untilDate">Required. ISO date yyyy-MM-dd (period end, inclusive).</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("generate_period_summary")]
public class GeneratePeriodSummarySkill : BaseSkillImplementation
{
    private readonly IGroupPlanningAgentRepository _planningAgentRepository;

    public GeneratePeriodSummarySkill(IGroupPlanningAgentRepository planningAgentRepository)
    {
        _planningAgentRepository = planningAgentRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");

        if (!DateOnly.TryParse(fromStr, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate: {fromStr}.");
        }
        if (!DateOnly.TryParse(untilStr, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate: {untilStr}.");
        }
        if (untilDate < fromDate)
        {
            return SkillResult.Error("untilDate must be on or after fromDate.");
        }

        var employeeIds = await _planningAgentRepository.GetAgentIdsAsync(groupId, fromDate, untilDate, cancellationToken);

        var summary = new
        {
            GroupId = groupId,
            FromDate = fromDate.ToString("yyyy-MM-dd"),
            UntilDate = untilDate.ToString("yyyy-MM-dd"),
            EmployeeCount = employeeIds.Count,
            EmployeeIds = employeeIds,
            DaysInPeriod = (untilDate.DayNumber - fromDate.DayNumber) + 1
        };

        return SkillResult.SuccessResult(
            summary,
            $"Period summary for group {groupId} between {fromDate:yyyy-MM-dd} and {untilDate:yyyy-MM-dd}: " +
            $"{employeeIds.Count} employee(s), {summary.DaysInPeriod} day(s).");
    }
}
