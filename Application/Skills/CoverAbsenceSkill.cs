// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reactive disruption flow: an employee falls out on a day — record the absence and propose
/// rule-compliant cover, all as one isolated scenario for approval (accept_scenario / reject_scenario).
/// Thin wrapper that dispatches <see cref="Klacks.Api.Application.Commands.Schedules.CoverAbsenceCommand"/>; the absence (Break) and,
/// per covered slot, either a move of the whole work to the stand-in or a Replacement WorkChange land in the scenario
/// (nothing touches the real schedule until accept). Which one is decided by CoverMoveDecision from the report time
/// against the work start. Locked slots of the absent employee are reported for manual review; slots without an
/// eligible candidate as under-coverage. The answer counts moved and replaced slots separately.
/// Report time and source of the replacement request book never come from the parameter dictionary: a set
/// SkillExecutionContext.InboundReceivedAtUtc (only the inbound orchestrator sets it) means Messenger at that
/// instant, everything else is RecoveryEngine reported now.
/// </summary>
/// <param name="clientId">Required. UUID of the employee who is absent.</param>
/// <param name="date">Required. First (or only) day of the absence in ISO yyyy-MM-dd.</param>
/// <param name="groupId">Required. UUID of the group / planning blade.</param>
/// <param name="absenceId">Required. UUID or name of the Absence type (sick/vacation/...); an unknown or ambiguous name answers with the real types instead of guessing.</param>
/// <param name="untilDate">Optional. Last day of a multi-day absence in ISO yyyy-MM-dd; omit for one day.</param>
/// <param name="overrideBlock">Optional. K1 supervisor override for a Block-mode compliance escalation (e.g. an emergency); default false.</param>
/// <param name="notifyEscalationRoster">Optional. Start the planner call list for the affected days; default true (unattended callers need a human).</param>

using Klacks.Api.Application.Commands.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("cover_absence")]
public class CoverAbsenceSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IAbsenceRepository _absenceRepository;

    public CoverAbsenceSkill(IMediator mediator, IAbsenceRepository absenceRepository)
    {
        _mediator = mediator;
        _absenceRepository = absenceRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var groupId = GetRequiredGuid(parameters, "groupId");
        var absenceInput = GetRequiredString(parameters, "absenceId");
        var isAbsenceUuid = Guid.TryParse(absenceInput, out _);
        var (absence, absenceError) = await AbsenceTypeResolver.ResolveAsync(
            isAbsenceUuid ? absenceInput : null,
            isAbsenceUuid ? null : absenceInput,
            _absenceRepository);
        if (absence is null)
        {
            return SkillResult.Error(absenceError!);
        }

        var date = GetParameter<DateOnly?>(parameters, "date")
            ?? throw new ArgumentException("Required parameter 'date' is missing");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate");
        var overrideBlock = GetParameter<bool?>(parameters, "overrideBlock") ?? false;
        var notifyEscalationRoster = GetParameter<bool?>(parameters, "notifyEscalationRoster") ?? true;
        var reportedAtUtc = context.InboundReceivedAtUtc;
        var source = reportedAtUtc.HasValue ? ReplacementRequestSource.Messenger : ReplacementRequestSource.RecoveryEngine;

        var outcome = await _mediator.Send(
            new CoverAbsenceCommand(
                clientId, date, groupId, absence.Id, untilDate, overrideBlock, context.UserLanguage, notifyEscalationRoster,
                reportedAtUtc, source),
            cancellationToken);

        var movedCount = outcome.Covered.Count(c => c.Kind == ReplacementRequestKind.Move);
        var replacedCount = outcome.Covered.Count - movedCount;
        var chainCount = outcome.Covered.Count(c => c.Relocations is { Count: > 0 });

        var data = new
        {
            outcome.ScenarioId,
            outcome.Token,
            outcome.ScenarioName,
            ClientId = clientId,
            Date = date.ToString(DateTimeFormats.IsoDate),
            UntilDate = untilDate?.ToString(DateTimeFormats.IsoDate),
            GroupId = groupId,
            CoveredCount = outcome.Covered.Count,
            MovedCount = movedCount,
            ReplacedCount = replacedCount,
            UncoveredCount = outcome.Uncovered.Count,
            Covered = outcome.Covered.Select(c => new
            {
                c.ShiftId,
                Date = c.Date.ToString(DateTimeFormats.IsoDate),
                c.ReplacementClientId,
                c.ReplacementName,
                Kind = c.Kind.ToString(),
                ChainMoves = (c.Relocations ?? []).Select(r => new
                {
                    r.ShiftId,
                    Date = r.Date.ToString(DateTimeFormats.IsoDate),
                    r.FromClientId,
                    r.FromName,
                    r.ToClientId,
                    r.ToName
                })
            }),
            Uncovered = outcome.Uncovered.Select(u => new
            {
                u.ShiftId,
                Date = u.Date.ToString(DateTimeFormats.IsoDate),
                u.Reason
            })
        };

        var message =
            $"Absence cover scenario '{outcome.ScenarioName}' ({outcome.ScenarioId}): " +
            $"{outcome.Covered.Count} slot(s) covered ({movedCount} moved whole to the stand-in, {replacedCount} kept with a replacement), " +
            $"{outcome.Uncovered.Count} uncovered (locked / no eligible candidate). Accepting removes an uncovered service only " +
            "if it has not started; a started one stays with a red notice. " +
            (chainCount > 0
                ? $"{chainCount} cover(s) work through a chain: the stand-in hands an own shift on (ChainMoves); every person named there has to agree. "
                : string.Empty) +
            "Review, then accept_scenario or reject_scenario.";

        return SkillResult.SuccessResult(data, message);
    }
}
