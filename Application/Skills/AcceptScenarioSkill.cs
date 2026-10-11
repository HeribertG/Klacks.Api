// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that accepts an AnalyseScenario — promotes its works/breaks to the main schedule via the
/// existing AcceptAnalyseScenarioCommand pipeline. Used after AutoWizard or any other scenario producer
/// has run and the user (or planning agent) wants to commit the proposed plan. A scenario outside the caller's
/// group visibility is refused by the command handler and reported exactly like a missing one.
/// When the original plan changed after the scenario was created (another scenario accepted, edits by hand), the
/// accept would overwrite those changes: the skill then stores the call under a one-time confirmation token and asks
/// the model to get the user's explicit consent first; only the confirmed replay (confirm_pending_action) accepts with
/// the stale override. The override is honoured only on a gate-bypassing replay, never on a direct model call, and the
/// token is marked as issued for a sensitive action this turn, so it cannot be redeemed in the same turn (as create_plan).
/// </summary>
/// <param name="scenarioId">UUID of the scenario to accept.</param>
/// <param name="confirmationStore">Mints the one-time token for the stale-scenario confirmation.</param>
/// <param name="companyClock">Resolves the company time zone the last change is reported in.</param>
/// <param name="turnScope">Marks that token as issued this turn, so it cannot be redeemed before the user answered.</param>

using System.Globalization;
using Klacks.Api.Application.Commands.AnalyseScenarios;
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
    private const string SkillName = "accept_scenario";

    private const string ScenarioIdParameter = "scenarioId";

    private const string CompanyTimeFormat = "yyyy-MM-dd HH:mm";

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
        var overrideStale = context.BypassAutonomyGate
            && GetParameter<bool>(parameters, ScenarioAcceptDefaults.StaleConfirmedParameter, false);

        bool success;
        try
        {
            success = await _mediator.Send(
                new AcceptAnalyseScenarioCommand(scenarioId, OverrideStale: overrideStale), cancellationToken);
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
            success = false;
        }

        if (!success)
        {
            return SkillResult.Error($"Failed to accept scenario {scenarioId} — it may already be accepted, rejected, or not exist.");
        }
        return SkillResult.SuccessResult(
            new { ScenarioId = scenarioId },
            $"Scenario {scenarioId} accepted — its works are now part of the main schedule.");
    }

    private async Task<SkillResult> RequireStaleConfirmationAsync(
        SkillExecutionContext context,
        IReadOnlyDictionary<string, object> parameters,
        Guid scenarioId,
        ScenarioStaleException exception,
        CancellationToken cancellationToken)
    {
        var pendingParameters = new Dictionary<string, object>(parameters)
        {
            [ScenarioAcceptDefaults.StaleConfirmedParameter] = ScenarioAcceptDefaults.ConfirmedValue
        };

        var token = _confirmationStore.Create(context.UserId, SkillName, pendingParameters);
        _turnScope.MarkIssuedForSensitiveSkill(token);
        _turnScope.MarkIssued(token);
        var staleness = exception.Staleness;
        var companyZone = await _companyClock.GetTimeZoneAsync(cancellationToken);
        var lastChange = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(staleness.LastChangeUtc, DateTimeKind.Utc), companyZone);
        var acceptedSince = staleness.AcceptedScenarioNames.Count == 0
            ? string.Empty
            : $" Scenarios accepted since then: {string.Join(", ", staleness.AcceptedScenarioNames)}.";

        var message =
            $"Scenario {scenarioId} was NOT accepted: the original plan was changed after this scenario was created " +
            $"({staleness.ChangedEntryCount} changed entries in its scope, last change {lastChange.ToString(CompanyTimeFormat, CultureInfo.InvariantCulture)} company time).{acceptedSince} " +
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
