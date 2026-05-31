// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reactive disruption flow: an employee falls out on a day — record the absence and propose
/// rule-compliant cover, all as one isolated scenario for approval (accept_scenario / reject_scenario).
/// Thin wrapper that dispatches <see cref="Klacks.Api.Application.Commands.Schedules.CoverAbsenceCommand"/>; the absence (Break) and a Replacement
/// WorkChange per slot land in the scenario (nothing touches the real schedule until accept). Locked
/// slots of the absent employee are reported for manual review; slots without an eligible candidate as
/// under-coverage. Use this when someone calls in sick / drops out and their shifts need covering.
/// </summary>
/// <param name="clientId">Required. UUID of the employee who is absent.</param>
/// <param name="date">Required. Day of the absence in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Required. UUID of the group / planning blade.</param>
/// <param name="absenceId">Required. UUID of the Absence type (sick/vacation/...).</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("cover_absence")]
public class CoverAbsenceSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public CoverAbsenceSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var groupId = GetRequiredGuid(parameters, "groupId");
        var absenceId = GetRequiredGuid(parameters, "absenceId");
        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");

        var outcome = await _mediator.Send(
            new CoverAbsenceCommand(clientId, date, groupId, absenceId), cancellationToken);

        var data = new
        {
            outcome.ScenarioId,
            outcome.Token,
            outcome.ScenarioName,
            ClientId = clientId,
            Date = date.ToString("yyyy-MM-dd"),
            GroupId = groupId,
            CoveredCount = outcome.Covered.Count,
            UncoveredCount = outcome.Uncovered.Count,
            Covered = outcome.Covered.Select(c => new
            {
                c.ShiftId,
                Date = c.Date.ToString("yyyy-MM-dd"),
                c.ReplacementClientId,
                c.ReplacementName
            }),
            Uncovered = outcome.Uncovered.Select(u => new
            {
                u.ShiftId,
                Date = u.Date.ToString("yyyy-MM-dd"),
                u.Reason
            })
        };

        var message =
            $"Absence cover scenario '{outcome.ScenarioName}' ({outcome.ScenarioId}): " +
            $"{outcome.Covered.Count} slot(s) covered, {outcome.Uncovered.Count} uncovered " +
            "(locked / no eligible candidate). Review, then accept_scenario or reject_scenario.";

        return SkillResult.SuccessResult(data, message);
    }
}
