// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Seals a billing period — the same group-aware path as the period-closing page: sets
/// LockLevel Closed on the works/breaks in range, creates the authoritative SealedDay
/// day locks and writes the period audit log. Without a group the seal is global. A seal
/// starts no payroll export; the person-based payroll export is run separately on the
/// period-closing page once every day of the period is locked. The group can be addressed
/// by UUID or by name. The seal is verified by re-reading the day-lock state afterwards.
/// </summary>
/// <param name="startDate">Period start in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="endDate">Period end in ISO yyyy-MM-dd (inclusive).</param>
/// <param name="groupId">Optional. UUID of the group to seal; takes precedence over groupName.</param>
/// <param name="groupName">Optional. Display name of the group; resolved with fuzzy matching.</param>
/// <param name="reason">Optional. Free-text reason stored in the period audit log.</param>
/// <param name="acknowledgeViolations">Optional, default false. True only after the user was shown the open errors and chose to seal anyway; without it a period that still holds errors is refused.</param>
/// <param name="acknowledgedErrorCount">Required when acknowledgeViolations is true. Number of errors the user was shown; the seal is refused again if the period holds more by now.</param>

using Klacks.Api.Application.Commands.PeriodClosing;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("close_period")]
public class ClosePeriodSkill : BaseSkillImplementation
{
    private const string AcknowledgeViolationsParameter = "acknowledgeViolations";
    private const string AcknowledgedErrorCountParameter = "acknowledgedErrorCount";
    private const string MissingErrorCountMessage =
        "Pass acknowledgedErrorCount = number of errors shown to the user together with acknowledgeViolations=true; " +
        "nothing was sealed.";

    private readonly IMediator _mediator;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;

    public ClosePeriodSkill(
        IMediator mediator,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard)
    {
        _mediator = mediator;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var startDate = GetParameter<DateOnly?>(parameters, "startDate")
            ?? throw new ArgumentException("Required parameter 'startDate' is missing");
        var endDate = GetParameter<DateOnly?>(parameters, "endDate")
            ?? throw new ArgumentException("Required parameter 'endDate' is missing");
        if (startDate > endDate)
        {
            return SkillResult.Error($"startDate ({startDate}) must be on or before endDate ({endDate}).");
        }

        var (groupId, groupName, groupError) = await PeriodClosingGroupResolver.ResolveAsync(
            GetParameter<string>(parameters, "groupId"),
            GetParameter<string>(parameters, "groupName"),
            _groupRepository, _groupScopeGuard, context, cancellationToken);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var reason = GetParameter<string>(parameters, "reason");

        var acknowledgeViolations = GetParameter<bool?>(parameters, AcknowledgeViolationsParameter) ?? false;
        var acknowledgedErrorCount = acknowledgeViolations
            ? GetParameter<int?>(parameters, AcknowledgedErrorCountParameter)
            : null;
        if (acknowledgeViolations && acknowledgedErrorCount == null)
        {
            return SkillResult.Error(MissingErrorCountMessage);
        }

        int count;
        try
        {
            count = await _mediator.Send(
                new ClosePeriodByGroupCommand(
                    startDate, endDate, groupId, reason, acknowledgeViolations, acknowledgedErrorCount),
                cancellationToken);
        }
        catch (PeriodValidationConflictException ex)
        {
            return SkillResult.Error(BuildViolationsRefusal(ex, acknowledgeViolations));
        }

        var days = await _mediator.Send(
            new GetSealedPeriodsQuery(startDate, endDate, groupId), cancellationToken);
        var sealedDays = days.Count(d => d.IsDaySealed);
        if (sealedDays == 0)
        {
            return SkillResult.Error(
                $"Database verification failed: no day locks found for {startDate}..{endDate} after the seal — " +
                "treat the period as not sealed.");
        }

        var scopeLabel = groupId.HasValue ? $"group '{groupName}'" : "ALL groups (global)";

        return SkillResult.SuccessResult(
            new
            {
                StartDate = startDate,
                EndDate = endDate,
                GroupId = groupId,
                GroupName = groupName,
                Reason = reason,
                AffectedItems = count,
                SealedDays = sealedDays
            },
            $"Sealed period {startDate}..{endDate} for {scopeLabel}: {count} item(s) locked, {sealedDays} day lock(s) " +
            "confirmed in the database (verified).");
    }

    private static string BuildViolationsRefusal(PeriodValidationConflictException ex, bool wasAcknowledged)
    {
        var fact = $"Nothing was sealed: the period holds {ex.CurrentErrorCount} unresolved error(s).";
        var next = wasAcknowledged
            ? "New findings appeared after the user's confirmation: show them to the user and ask again; seal only if " +
              "the user explicitly decides to seal anyway."
            : "Show the user the open errors. Only if the user explicitly decides to seal anyway, call close_period " +
              $"again with {AcknowledgeViolationsParameter}=true and {AcknowledgedErrorCountParameter}={ex.CurrentErrorCount}.";
        return $"{fact} {next}";
    }
}
