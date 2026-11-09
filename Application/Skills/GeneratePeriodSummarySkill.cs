// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Generates a textual period summary (employee count, total target hours, computed hours
/// and drift) for a given group + period. Klacksy uses this for chat answers like
/// "give me the May summary for Bern" or before triggering email_schedule_to_client.
/// A group outside the caller's group scope is answered exactly like a missing one. Stand-ins are planned in the group
/// but are no employees, so they are left out of the employee count and list.
/// </summary>
/// <param name="groupId">Required. UUID of the group / location.</param>
/// <param name="fromDate">Required. ISO date yyyy-MM-dd (period start).</param>
/// <param name="untilDate">Required. ISO date yyyy-MM-dd (period end, inclusive).</param>
/// <param name="planningAgentRepository">Resolves the employees planned in the group for the period</param>
/// <param name="groupRepository">Loads the requested group for the scope check</param>
/// <param name="clientRepository">Tells which of the planned clients are stand-ins</param>
/// <param name="groupScopeGuard">Resolves the executing user's group scope</param>

using System.Globalization;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("generate_period_summary")]
public class GeneratePeriodSummarySkill : BaseSkillImplementation
{
    private readonly IGroupPlanningAgentRepository _planningAgentRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IClientRepository _clientRepository;

    public GeneratePeriodSummarySkill(
        IGroupPlanningAgentRepository planningAgentRepository,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IClientRepository clientRepository)
    {
        _planningAgentRepository = planningAgentRepository;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _clientRepository = clientRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");

        if (!SkillCalendarStringParser.TryParseDateOnly(fromStr, context.UserLanguage, out var fromDate))
        {
            return SkillResult.Error($"Invalid fromDate: {fromStr}.");
        }
        if (!SkillCalendarStringParser.TryParseDateOnly(untilStr, context.UserLanguage, out var untilDate))
        {
            return SkillResult.Error($"Invalid untilDate: {untilStr}.");
        }
        if (untilDate < fromDate)
        {
            return SkillResult.Error("untilDate must be on or after fromDate.");
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsUnrestricted)
        {
            var group = await ScopedGroupLookup.FindAsync(_groupRepository, scope, groupId);
            if (group == null)
            {
                return SkillResult.Error($"Group with ID '{groupId}' not found.");
            }
        }

        var agentIds = await _planningAgentRepository.GetAgentIdsAsync(groupId, fromDate, untilDate, cancellationToken);
        var standInIds = await _clientRepository.GetStandInIdsAsync(agentIds, cancellationToken);
        var employeeIds = agentIds.Where(id => !standInIds.Contains(id)).ToList();

        var summary = new
        {
            GroupId = groupId,
            FromDate = fromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            UntilDate = untilDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            EmployeeCount = employeeIds.Count,
            EmployeeIds = employeeIds,
            DaysInPeriod = (untilDate.DayNumber - fromDate.DayNumber) + 1
        };

        return SkillResult.SuccessResult(
            summary,
            $"Period summary for group {groupId} between {fromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)} and {untilDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}: " +
            $"{employeeIds.Count} employee(s), {summary.DaysInPeriod} day(s).");
    }
}
