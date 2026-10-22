// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that accepts an AnalyseScenario — promotes its works/breaks to the main schedule via the
/// existing AcceptAnalyseScenarioCommand pipeline. Used after AutoWizard or any other scenario producer
/// has run and the user (or planning agent) wants to commit the proposed plan. A scenario outside the caller's
/// group visibility is refused by the command handler and reported exactly like a missing one.
/// When the original plan changed after the scenario was created (another scenario accepted, edits, seals or a background recalculation), the
/// accept would overwrite those changes: the skill then stores the call under a one-time confirmation token and asks
/// the model to get the user's explicit consent first. The token has its own purpose (StaleScenarioOverwrite) and keeps
/// the staleness the user is shown; only redeeming it sets SkillExecutionContext.StaleScenarioOverwriteAcknowledgement,
/// the sole source of the stale override - no parameter and no gate bypass (plan step, scheduled task, inbound,
/// proactive) can supply it. The accept re-checks the staleness against that acknowledgement and asks again when the
/// plan changed further meanwhile. The token is marked as issued for a sensitive action this turn, so it cannot be
/// redeemed in the same turn (as create_plan). Entries on locked days are not taken over by the accept; their number is
/// reported in the success message so the model can tell the user.
/// </summary>
/// <param name="scenarioId">UUID of the scenario to accept.</param>
/// <param name="confirmationStore">Mints the one-time token for the stale-scenario confirmation.</param>
/// <param name="companyClock">Resolves the company time zone the last change is reported in.</param>
/// <param name="turnScope">Marks that token as issued this turn, so it cannot be redeemed before the user answered.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class AcceptScenarioSkill : BaseSkillImplementation
{
    private const string SkillName = ScenarioAcceptDefaults.SkillName;

    private const string ScenarioIdParameter = "scenarioId";

    private readonly IMediator _mediator;
    private readonly IPendingConfirmationStore _confirmationStore;
    private readonly ITurnConfirmationScope _turnScope;
    private readonly ICompanyClock _companyClock;

    public AcceptScenarioSkill(
        IMediator mediator,
        IPendingConfirmationStore confirmationStore,
        ITurnConfirmationScope turnScope,
        ICompanyClock companyClock)
    {
        _mediator = mediator;
        _confirmationStore = confirmationStore;
        _turnScope = turnScope;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var scenarioId = GetRequiredGuid(parameters, ScenarioIdParameter);
        var acknowledgement = context.StaleScenarioOverwriteAcknowledgement;

        AcceptAnalyseScenarioResult? result;
        try
        {
            result = await _mediator.Send(
                new AcceptAnalyseScenarioCommand(
                    scenarioId,
                    OverrideStale: acknowledgement is not null,
                    AcknowledgedStaleness: acknowledgement),
                cancellationToken);
        }
        catch (ScenarioStaleException ex)
        {
            return await RequireStaleConfirmationAsync(context, parameters, scenarioId, ex, cancellationToken);
        }
        catch (ConflictException ex)
        {
            return SkillResult.Error($"Failed to accept scenario {scenarioId}: {ex.Message}");
        }
        catch (KeyNotFoundException)
        {
            result = null;
        }

        if (result is not { Accepted: true })
        {
            return SkillResult.Error($"Failed to accept scenario {scenarioId} — it may already be accepted, rejected, or not exist.");
        }

        var skipped = result.SkippedLockedTotal;
        var lockedHint = skipped > 0
            ? $" {skipped} scenario entries on locked days (sealed or locked entries) were NOT taken over; the original plan stays there. Tell the user."
            : string.Empty;
        return SkillResult.SuccessResult(
            new { ScenarioId = scenarioId, SkippedLockedEntries = skipped },
            $"Scenario {scenarioId} accepted — its works are now part of the main schedule.{lockedHint}");
    }

    private async Task<SkillResult> RequireStaleConfirmationAsync(
        SkillExecutionContext context,
        IReadOnlyDictionary<string, object> parameters,
        Guid scenarioId,
        ScenarioStaleException exception,
        CancellationToken cancellationToken)
    {
        var staleness = exception.Staleness;
        var pendingParameters = new Dictionary<string, object>(parameters);
        foreach (var reserved in ScenarioAcceptDefaults.ReservedParameters)
        {
            pendingParameters.Remove(reserved);
        }

        pendingParameters[ScenarioAcceptDefaults.AcknowledgedChangedEntryCountParameter] =
            staleness.ChangedEntryCount.ToString(CultureInfo.InvariantCulture);
        pendingParameters[ScenarioAcceptDefaults.AcknowledgedLastChangeUtcTicksParameter] =
            DateTime.SpecifyKind(staleness.LastChangeUtc, DateTimeKind.Utc).Ticks.ToString(CultureInfo.InvariantCulture);

        var token = _confirmationStore.Create(
            context.UserId, SkillName, pendingParameters, PendingConfirmationPurposes.StaleScenarioOverwrite,
            context.ConfirmationChannel);
        _turnScope.MarkIssued(token);
        var companyZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var lastChange = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(staleness.LastChangeUtc, DateTimeKind.Utc), companyZone);
        var acceptedSince = staleness.AcceptedScenarioNames.Count == 0
            ? string.Empty
            : $" Scenarios accepted since then: {string.Join(", ", staleness.AcceptedScenarioNames)}.";

        var message =
            $"Scenario {scenarioId} was NOT accepted: the original plan was changed after this scenario was created " +
            $"({staleness.ChangedEntryCount} changed entries in its scope, last change {lastChange.ToString(DateTimeFormats.IsoDateHourMinute, CultureInfo.InvariantCulture)} company time).{acceptedSince} " +
            "Accepting replaces the whole scenario period of the original plan with the scenario, so those newer " +
            "changes (for example an accepted replacement or edits by hand) would be lost. The action is stored and " +
            "NOT executed yet. Explain this to the user and ask whether they want to accept anyway and overwrite the " +
            "changes; only after they confirmed in their own words, call " +
            $"'{AutonomyDefaults.ConfirmPendingActionSkillName}' with " +
            $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to '{token}'. Never confirm on your own.";

        return SkillResult.Confirmation(
            message,
            token,
            new
            {
                ScenarioId = scenarioId,
                ErrorCode = ScenarioStaleException.ErrorCode,
                staleness.ChangedEntryCount,
                staleness.LastChangeUtc,
                staleness.AcceptedScenarioNames
            });
    }
}
