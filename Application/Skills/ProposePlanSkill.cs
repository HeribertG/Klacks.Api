// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Materializes a set of agent-chosen placements into an isolated AnalyseScenario for supervised
/// approval, then leaves it for accept_scenario / reject_scenario. This does NOT find gaps — the agent
/// chooses the placements (e.g. via read_schedule_state + find_replacement); for bulk auto-fill use
/// start_autowizard. The real schedule is cloned under the scenario token first, the pre-commit
/// guardrail keeps the written scenario collision-free, and any placement that would collide is skipped
/// and returned in the rejected list (it never silently drops work or corrupts real data). A group outside the
/// caller's visibility is reported exactly like a missing group.
/// </summary>
/// <param name="groupId">Required. UUID of the group / planning blade the scenario belongs to.</param>
/// <param name="fromDate">Required. ISO date yyyy-MM-dd (scenario period start).</param>
/// <param name="untilDate">Required. ISO date yyyy-MM-dd (scenario period end, inclusive).</param>
/// <param name="placements">Required. JSON array of {clientId, shiftId, date} placements to propose.</param>

using System.Globalization;
using System.Text.Json;
using Klacks.Api.Application.Helpers;
using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("propose_plan")]
public class ProposePlanSkill : BaseSkillImplementation
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IMediator _mediator;

    public ProposePlanSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var fromStr = GetRequiredString(parameters, "fromDate");
        var untilStr = GetRequiredString(parameters, "untilDate");
        var placementsJson = GetRequiredString(parameters, "placements");

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

        List<PlacementJson>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<List<PlacementJson>>(placementsJson, JsonOptions);
        }
        catch (JsonException)
        {
            return SkillResult.Error("Invalid placements: expected a JSON array of {clientId, shiftId, date}.");
        }

        if (raw == null || raw.Count == 0)
        {
            return SkillResult.Error("placements must contain at least one {clientId, shiftId, date} entry.");
        }

        var placements = new List<PlacementInput>(raw.Count);
        foreach (var entry in raw)
        {
            if (!Guid.TryParse(entry.ClientId, out var clientId))
            {
                return SkillResult.Error($"Invalid clientId in placements: {entry.ClientId}.");
            }
            if (!Guid.TryParse(entry.ShiftId, out var shiftId))
            {
                return SkillResult.Error($"Invalid shiftId in placements: {entry.ShiftId}.");
            }
            if (!SkillCalendarStringParser.TryParseDateOnly(entry.Date, context.UserLanguage, out var date))
            {
                return SkillResult.Error($"Invalid date in placements: {entry.Date}.");
            }
            if (date < fromDate || date > untilDate)
            {
                return SkillResult.Error($"Placement date {entry.Date} is outside the period {fromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}..{untilDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}.");
            }
            placements.Add(new PlacementInput(clientId, shiftId, date));
        }

        ProposePlanOutcome outcome;
        try
        {
            outcome = await _mediator.Send(
                new ProposePlanCommand(groupId, fromDate, untilDate, placements, Language: context.UserLanguage), cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var data = new
        {
            outcome.ScenarioId,
            outcome.Token,
            outcome.ScenarioName,
            GroupId = groupId,
            FromDate = fromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            UntilDate = untilDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
            WrittenCount = outcome.Written.Count,
            RejectedCount = outcome.Rejected.Count,
            WarningCount = outcome.Warnings.Count,
            Written = outcome.Written.Select(w => new
            {
                w.ClientId,
                w.ShiftId,
                Date = w.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)
            }),
            Rejected = outcome.Rejected.Select(r => new
            {
                r.ClientId,
                r.ShiftId,
                Date = r.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                r.Reason
            }),
            Warnings = outcome.Warnings.Select(c => new
            {
                Severity = c.Type.ToString(),
                c.Comment,
                Date = c.Date.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture),
                c.ClientId,
                CommentParams = LocalizedCommentParams.ForLanguage(c.CommentParams, context.UserLanguage)
            })
        };

        var message =
            $"Proposed plan scenario '{outcome.ScenarioName}' ({outcome.ScenarioId}): " +
            $"{outcome.Written.Count} placement(s) written, {outcome.Rejected.Count} rejected, " +
            $"{outcome.Warnings.Count} warning(s). Review, then accept_scenario or reject_scenario.";

        return SkillResult.SuccessResult(data, message);
    }

    private sealed record PlacementJson(string ClientId, string ShiftId, string Date);
}
